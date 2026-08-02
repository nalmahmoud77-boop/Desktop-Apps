using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

// ============================================================================
//  AgileFlow License Key Tool  (VENDOR-ONLY — keep the private key secret!)
//
//  Commands:
//     dotnet run --project tools/LicenseKeyTool -- keygen
//         Creates the ECDSA signing key pair (once) and prints the PUBLIC key.
//         Copy the printed public key into LicenseKey.PublicKeyB64 in the app.
//
//     dotnet run --project tools/LicenseKeyTool -- pubkey
//         Re-print the public key from the existing private key file.
//
//     dotnet run --project tools/LicenseKeyTool -- machineid
//         Print THIS machine's fingerprint (for issuing a bound key).
//
//     dotnet run --project tools/LicenseKeyTool -- mint <monthly|yearly|full>
//                 [--machine <id>|--any] [--days N]
//         Mint a signed license key. Defaults to binding to THIS machine.
//
//  LICENSE KEY FORMAT (v1) — MUST stay in sync with the app's LicenseKey.cs:
//     payload = "AGF1|{id}|{TIER}|{issuedUnix}|{expiresUnix}|{machineId}"
//     key     = base64url(payloadUtf8) + "." + base64url(signature)
//     signature = ECDSA P-256 SignData(payload, SHA-256)  [IEEE P1363, 64 bytes]
// ============================================================================

// Resolve the signing key next to the tool's binary so it works regardless of
// the current working directory (dotnet run resolves relative paths against CWD).
string KeyFile = Path.Combine(AppContext.BaseDirectory, "signing-key.pkcs8.b64");

var cmd = (args.Length > 0 ? args[0] : "").ToLowerInvariant();

switch (cmd)
{
    case "keygen": KeyGen(); break;
    case "pubkey": PubKey(); break;
    case "machineid": Console.WriteLine(GetMachineId()); break;
    case "mint": Mint(args); break;
    case "verify": Verify(args); break;
    default:
        Console.WriteLine("Usage: keygen | pubkey | machineid | mint <monthly|yearly|full> [--machine <id>|--any] [--days N]");
        break;
}

void KeyGen()
{
    if (File.Exists(KeyFile))
    {
        Console.WriteLine($"Key file already exists ({Path.GetFullPath(KeyFile)}). Printing public key:\n");
        PubKey();
        return;
    }
    using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    File.WriteAllText(KeyFile, Convert.ToBase64String(ec.ExportPkcs8PrivateKey()));
    Console.WriteLine("Created private signing key: " + Path.GetFullPath(KeyFile));
    Console.WriteLine("*** KEEP THIS FILE SECRET AND BACKED UP. Do NOT ship it. ***\n");
    Console.WriteLine("PUBLIC KEY (paste into LicenseKey.PublicKeyB64 in the app):\n");
    Console.WriteLine(Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo()));
}

void PubKey()
{
    using var ec = LoadPrivate();
    Console.WriteLine(Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo()));
}

void Mint(string[] a)
{
    if (a.Length < 2) { Console.WriteLine("mint <monthly|yearly|full> [--machine <id>|--any] [--days N]"); return; }

    string tierArg = a[1].ToLowerInvariant();
    string tier = tierArg switch
    {
        "monthly" => "MONTHLY",
        "yearly" => "YEARLY",
        "full" => "FULL",
        _ => throw new ArgumentException("tier must be monthly, yearly or full")
    };

    int? days = tier == "MONTHLY" ? 30 : tier == "YEARLY" ? 365 : (int?)null;
    string machine = GetMachineId();

    for (int i = 2; i < a.Length; i++)
    {
        if (a[i] == "--any") machine = "ANY";
        else if (a[i] == "--machine" && i + 1 < a.Length) machine = a[++i].ToUpperInvariant();
        else if (a[i] == "--days" && i + 1 < a.Length) days = int.Parse(a[++i]);
    }

    var nowUtc = DateTimeOffset.UtcNow;
    long issued = nowUtc.ToUnixTimeSeconds();
    long expires = days.HasValue ? nowUtc.AddDays(days.Value).ToUnixTimeSeconds() : 0;
    string id = Guid.NewGuid().ToString("N");

    string payload = $"AGF1|{id}|{tier}|{issued}|{expires}|{machine}";
    byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

    using var ec = LoadPrivate();
    byte[] sig = ec.SignData(payloadBytes, HashAlgorithmName.SHA256);

    string key = B64Url(payloadBytes) + "." + B64Url(sig);

    Console.WriteLine($"Tier      : {tier}");
    Console.WriteLine($"Machine   : {machine}");
    Console.WriteLine($"Issued    : {nowUtc:u}");
    Console.WriteLine($"Expires   : {(expires == 0 ? "never (perpetual)" : DateTimeOffset.FromUnixTimeSeconds(expires).ToString("u"))}");
    Console.WriteLine("\nLICENSE KEY:\n");
    Console.WriteLine(key);
}

// Verify a key exactly the way the app does: against the EMBEDDED public key,
// using the same base64url + IEEE-P1363 parsing. Confirms app/tool are in sync.
void Verify(string[] a)
{
    const string EmbeddedPublicKeyB64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE5YrlYPUj+Zzg6RBnQvxPQbImjSdgEdNQNZN7qQEDQFkck6OfU7sUVbFBppLcFovA4vMvaW7WKcNkAk+fSbRKrg==";

    if (a.Length < 2) { Console.WriteLine("verify <key>"); return; }
    string key = new string(a[1].Where(c => !char.IsWhiteSpace(c)).ToArray());
    int dot = key.IndexOf('.');
    if (dot <= 0) { Console.WriteLine("FAIL: malformed (no separator)"); return; }

    byte[] payload = FromB64Url(key.Substring(0, dot));
    byte[] sig = FromB64Url(key.Substring(dot + 1));

    using var ec = ECDsa.Create();
    ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(EmbeddedPublicKeyB64), out _);
    bool ok = ec.VerifyData(payload, sig, HashAlgorithmName.SHA256);
    Console.WriteLine(ok
        ? "OK  signature valid  ->  " + Encoding.UTF8.GetString(payload)
        : "FAIL signature INVALID");
}

byte[] FromB64Url(string s)
{
    string b64 = s.Replace('-', '+').Replace('_', '/');
    switch (b64.Length % 4) { case 2: b64 += "=="; break; case 3: b64 += "="; break; }
    return Convert.FromBase64String(b64);
}

ECDsa LoadPrivate()
{
    if (!File.Exists(KeyFile))
        throw new FileNotFoundException($"Private key not found. Run 'keygen' first. ({Path.GetFullPath(KeyFile)})");
    var ec = ECDsa.Create();
    ec.ImportPkcs8PrivateKey(Convert.FromBase64String(File.ReadAllText(KeyFile).Trim()), out _);
    return ec;
}

static string GetMachineId()
{
    string raw;
    try
    {
        using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        raw = k?.GetValue("MachineGuid") as string ?? Environment.MachineName;
    }
    catch { raw = Environment.MachineName; }

    using var sha = SHA256.Create();
    byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes("AGF|" + raw));
    var sb = new StringBuilder(16);
    for (int i = 0; i < 8; i++) sb.Append(h[i].ToString("X2"));
    return sb.ToString();
}

static string B64Url(byte[] data) =>
    Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

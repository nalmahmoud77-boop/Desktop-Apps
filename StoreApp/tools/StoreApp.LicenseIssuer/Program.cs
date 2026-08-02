using System.Security.Cryptography;
using StoreApp.LicenseIssuer;

if (args.Length == 0) { PrintUsage(); return 1; }

return args[0].ToLowerInvariant() switch
{
    "genkey" => GenKey(args[1..]),
    "issue"  => Issue(args[1..]),
    "verify" => Verify(args[1..]),
    _ => Fail()
};

static int Fail() { PrintUsage(); return 1; }

static int GenKey(string[] args)
{
    var outDir = ArgValue(args, "--out") ?? "license-keys";
    Directory.CreateDirectory(outDir);

    using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    var privPath = Path.Combine(outDir, "private.pem");
    var pubPath  = Path.Combine(outDir, "public.pem");

    File.WriteAllText(privPath, ec.ExportECPrivateKeyPem());
    File.WriteAllText(pubPath,  ec.ExportSubjectPublicKeyInfoPem());

    var pubB64 = Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo());

    Console.WriteLine($"Wrote {privPath}");
    Console.WriteLine($"Wrote {pubPath}");
    Console.WriteLine();
    Console.WriteLine("Paste this into EcdsaLicenseService.PublicKeyBase64:");
    Console.WriteLine();
    Console.WriteLine(pubB64);
    Console.WriteLine();
    Console.WriteLine("Keep private.pem OFFLINE. Never commit it.");
    return 0;
}

static int Issue(string[] args)
{
    var keyPath = ArgValue(args, "--key") ?? "license-keys/private.pem";
    var plan    = ArgValue(args, "--plan");
    var email   = ArgValue(args, "--email");
    var owner   = ArgValue(args, "--owner") ?? email ?? string.Empty;
    var daysStr = ArgValue(args, "--days");
    var outPath = ArgValue(args, "--out");

    if (plan is null || email is null) { PrintUsage(); return 1; }
    if (!File.Exists(keyPath)) { Console.Error.WriteLine($"Private key not found: {keyPath}"); return 2; }

    var planNorm = NormalizePlan(plan);
    if (planNorm is null) { Console.Error.WriteLine($"Unknown plan: {plan}"); return 2; }

    DateTime? expires = planNorm == "Lifetime"
        ? null
        : DateTime.UtcNow.AddDays(int.TryParse(daysStr, out var n) ? n : DefaultDays(planNorm));

    using var ec = ECDsa.Create();
    ec.ImportFromPem(File.ReadAllText(keyPath));

    var payload = new LicensePayload
    {
        Id = Guid.NewGuid().ToString("N"),
        Email = email,
        Owner = owner,
        Plan = planNorm,
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = expires
    };

    var token = LicenseToken.Encode(payload, ec);

    if (outPath is null) Console.WriteLine(token);
    else { File.WriteAllText(outPath, token); Console.WriteLine($"Wrote {outPath}"); }

    Console.Error.WriteLine();
    Console.Error.WriteLine($"License ID: {payload.Id}");
    Console.Error.WriteLine($"Plan:       {payload.Plan}");
    Console.Error.WriteLine($"Email:      {payload.Email}");
    Console.Error.WriteLine($"Issued:     {payload.IssuedAt:O}");
    Console.Error.WriteLine($"Expires:    {(payload.ExpiresAt?.ToString("O") ?? "never")}");
    Console.Error.WriteLine($"Token size: {token.Length} chars");
    return 0;
}

static int Verify(string[] args)
{
    var pubPath = ArgValue(args, "--key") ?? "license-keys/public.pem";
    var token   = ArgValue(args, "--token");
    var tokenFile = ArgValue(args, "--file");

    if (token is null && tokenFile is null) { PrintUsage(); return 1; }
    if (token is null) token = File.ReadAllText(tokenFile!).Trim();

    using var ec = ECDsa.Create();
    ec.ImportFromPem(File.ReadAllText(pubPath));

    var parts = token.Split('.');
    if (parts.Length != 3 || parts[0] != "STORE1") { Console.Error.WriteLine("Bad format"); return 2; }

    byte[] json, sig;
    try { json = FromBase64Url(parts[1]); sig = FromBase64Url(parts[2]); }
    catch { Console.Error.WriteLine("Bad base64"); return 2; }

    var ok = ec.VerifyData(json, sig, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    if (!ok) { Console.Error.WriteLine("INVALID signature"); return 3; }

    Console.WriteLine("OK");
    Console.WriteLine(System.Text.Encoding.UTF8.GetString(json));
    return 0;
}

static byte[] FromBase64Url(string s)
{
    s = s.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
    return Convert.FromBase64String(s);
}

static string? NormalizePlan(string s) => s.Trim().ToLowerInvariant() switch
{
    "monthly"  => "Monthly",
    "yearly"   => "Yearly",
    "lifetime" => "Lifetime",
    _ => null
};

static int DefaultDays(string plan) => plan switch
{
    "Monthly" => 30,
    "Yearly"  => 365,
    _ => 30
};

static string? ArgValue(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return null;
}

static void PrintUsage()
{
    Console.WriteLine("storeapp-issuer <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  genkey [--out <dir>]");
    Console.WriteLine("      Generate ECDSA P-256 keypair (private.pem + public.pem).");
    Console.WriteLine();
    Console.WriteLine("  issue --plan <Monthly|Yearly|Lifetime> --email <addr>");
    Console.WriteLine("        [--owner <name>] [--days <n>] [--key <path>] [--out <file>]");
    Console.WriteLine("      Issue a signed license token. Default key path: license-keys/private.pem");
    Console.WriteLine();
    Console.WriteLine("  verify [--token <s> | --file <path>] [--key <public.pem>]");
    Console.WriteLine("      Verify a license token against the public key.");
}

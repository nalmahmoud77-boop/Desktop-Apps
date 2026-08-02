namespace PersonalTaskManagement.Licensing
{
    public enum LicenseTier
    {
        Monthly,
        Yearly,
        Full
    }

    public enum LicenseState
    {
        /// <summary>No license key stored yet.</summary>
        NotActivated,
        /// <summary>Key is well-formed, signed, on the right machine, and not expired.</summary>
        Valid,
        /// <summary>Signature/machine are fine but the term has ended (Monthly/Yearly).</summary>
        Expired,
        /// <summary>The key text is not in a recognizable format.</summary>
        Malformed,
        /// <summary>The signature does not verify — forged or corrupted key.</summary>
        InvalidSignature,
        /// <summary>The key is valid but was issued for a different machine.</summary>
        WrongMachine
    }
}

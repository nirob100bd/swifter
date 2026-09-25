using System.Security.Cryptography.X509Certificates;

namespace Swifter.Core.Network;

public sealed class CertificateManager
{
    private static CertificateManager? _instance;
    private readonly List<CertificateException> _exceptions = new();
    private readonly List<X509Certificate2> _customCas = new();

    public static CertificateManager Instance => _instance ??= new CertificateManager();

    public IReadOnlyList<CertificateException> Exceptions => _exceptions.AsReadOnly();
    public IReadOnlyList<X509Certificate2> CustomCas => _customCas.AsReadOnly();

    private CertificateManager()
    {
        LoadExceptions();
    }

    private void LoadExceptions()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "cert_exceptions.json");
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var exceptions = System.Text.Json.JsonSerializer.Deserialize<List<CertificateException>>(json);
                if (exceptions != null) _exceptions.AddRange(exceptions);
            }
            catch
            {
            }
        }
    }

    public CertificateValidationResult ValidateCertificate(X509Certificate2 certificate, string hostname)
    {
        var result = new CertificateValidationResult { IsValid = true };

        if (certificate.NotAfter < DateTime.Now)
        {
            result.IsValid = false;
            result.Error = "Certificate has expired";
            result.ErrorCode = CertificateError.Expired;
        }
        else if (certificate.NotBefore > DateTime.Now)
        {
            result.IsValid = false;
            result.Error = "Certificate is not yet valid";
            result.ErrorCode = CertificateError.NotYetValid;
        }

        var cn = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (!string.IsNullOrEmpty(hostname) && !MatchHostname(cn, hostname))
        {
            var san = certificate.GetNameInfo(X509NameType.DnsName, false);
            if (!MatchHostname(san, hostname))
            {
                result.IsValid = false;
                result.Error = "Certificate hostname mismatch";
                result.ErrorCode = CertificateError.HostnameMismatch;
            }
        }

        if (!result.IsValid)
        {
            if (_exceptions.Any(e => e.Hostname == hostname && e.ExpiresAt > DateTime.UtcNow))
            {
                result.IsValid = true;
                result.WasExceptionApplied = true;
            }
        }

        result.Certificate = new CertificateInfo
        {
            Subject = certificate.Subject,
            Issuer = certificate.Issuer,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            Thumbprint = certificate.Thumbprint,
            SerialNumber = certificate.SerialNumber,
            SignatureAlgorithm = certificate.SignatureAlgorithm.FriendlyName ?? ""
        };

        return result;
    }

    private static bool MatchHostname(string pattern, string hostname)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(hostname)) return false;
        if (pattern.StartsWith("*."))
        {
            var suffix = pattern[2..];
            return hostname.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                   hostname.Count(c => c == '.') == suffix.Count(c => c == '.') + 1;
        }
        return pattern.Equals(hostname, StringComparison.OrdinalIgnoreCase);
    }

    public void AddException(string hostname, string reason, TimeSpan duration)
    {
        _exceptions.RemoveAll(e => e.Hostname == hostname);
        _exceptions.Add(new CertificateException
        {
            Hostname = hostname,
            Reason = reason,
            AddedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        });
        SaveExceptions();
    }

    public void RemoveException(string hostname)
    {
        _exceptions.RemoveAll(e => e.Hostname == hostname);
        SaveExceptions();
    }

    public void ImportCustomCa(string certPath, string? password = null)
    {
        var cert = new X509Certificate2(certPath, password);
        _customCas.Add(cert);
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(cert);
        store.Close();
    }

    private void SaveExceptions()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "cert_exceptions.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(_exceptions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<CertificateInfo> GetInstalledCertificates()
    {
        var certs = new List<CertificateInfo>();
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        foreach (var cert in store.Certificates)
        {
            certs.Add(new CertificateInfo
            {
                Subject = cert.Subject,
                Issuer = cert.Issuer,
                NotBefore = cert.NotBefore,
                NotAfter = cert.NotAfter,
                Thumbprint = cert.Thumbprint,
                SerialNumber = cert.SerialNumber,
                SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName ?? ""
            });
        }
        return certs;
    }
}

public sealed class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = "";
    public CertificateError ErrorCode { get; set; }
    public bool WasExceptionApplied { get; set; }
    public CertificateInfo Certificate { get; set; } = new();
}

public sealed class CertificateInfo
{
    public string Subject { get; set; } = "";
    public string Issuer { get; set; } = "";
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public string Thumbprint { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string SignatureAlgorithm { get; set; } = "";
}

public sealed class CertificateException
{
    public string Hostname { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime AddedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public enum CertificateError
{
    None,
    Expired,
    NotYetValid,
    HostnameMismatch,
    UntrustedRoot,
    Revoked,
    Unknown
}
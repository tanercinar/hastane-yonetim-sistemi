namespace HospitalManagement.BuildingBlocks.Security;

public static class SecurityHeaderDefaults
{
    public const string XContentTypeOptions = "nosniff";
    public const string XFrameOptions = "DENY";
    public const string ReferrerPolicy = "strict-origin-when-cross-origin";
    public const string XXssProtection = "0";

    public const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self' wss: ws:; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
}

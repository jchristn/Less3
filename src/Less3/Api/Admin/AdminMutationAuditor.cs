namespace Less3.Api.Admin
{
    using System;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;

    /// <summary>
    /// Persists audit records for sensitive administrative mutations.
    /// </summary>
    internal static class AdminMutationAuditor
    {
        internal static void Record(
            ConfigManager config,
            LoggingModule logging,
            S3Context ctx,
            string tenantId,
            string resourceType,
            string resourceId,
            string operation)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            try
            {
                AuthorizationAudit audit = new AuthorizationAudit();
                audit.TenantId = String.IsNullOrEmpty(tenantId) ? "default" : tenantId;
                audit.ResourceType = resourceType;
                audit.ResourceId = resourceId;
                audit.Operation = operation;
                audit.Permitted = true;

                RequestContext requestContext = ctx?.Metadata as RequestContext;
                if (requestContext != null)
                {
                    audit.UserId = requestContext.UserId;
                    audit.CredentialId = requestContext.CredentialId;
                    audit.Reason = "Admin session mutation.";
                }
                else
                {
                    audit.Reason = "Admin API key mutation.";
                }

                config.AddAuthorizationAudit(audit);
            }
            catch (Exception e)
            {
                logging?.Debug("Failed to persist admin mutation audit: " + e.Message);
            }
        }
    }
}

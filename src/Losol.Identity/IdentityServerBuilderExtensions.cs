using IdentityModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;

namespace Losol.Identity
{
    public static class IdentityServerBuilderExtensions
    {
        public static void AddSigningCredentialFromLocalMachineStorage(this IIdentityServerBuilder builder, string commonName)
        {
            //The one that expires last at the top
            var certs = X509.LocalMachine.My.SubjectDistinguishedName.Find("CN=" + commonName, false)
                .Where(o => DateTime.UtcNow >= o.NotBefore)
                .OrderByDescending(o => o.NotAfter)
                .ToArray();

            if (!certs.Any())
            {
                throw new Exception("No valid certificates could be found.");
            }

            //Get first (in desc order of expiry) th
            var signingCert = certs.FirstOrDefault();
            if (signingCert == null)
            {
                throw new InvalidOperationException("No valid signing certificate could be found.");
            }

            var signingCredential = new SigningCredentials(new X509SecurityKey(signingCert), "RS256");
            builder.AddSigningCredential(signingCredential);

            foreach (var cert in certs)
            {
                builder.AddValidationKey(cert);
            }
        }
    }
}

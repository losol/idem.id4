namespace Losol.Identity.Util
{
    public static class UriUtil
    {
        public static string BuildUri(string path, string baseUri)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            baseUri = baseUri.Trim();
            path = path.Trim();
            return $"{baseUri.TrimEnd('/')}/{path.TrimStart('/')}";

        }
    }
}

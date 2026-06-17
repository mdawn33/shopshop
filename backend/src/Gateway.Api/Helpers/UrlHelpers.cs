namespace Gateway.Api.Helpers;

public class UrlHelpers
{
 /// <summary>
 /// Prevents Open Redirect vulnerabilities.
 /// </summary>
 /// <param name="url"></param>
 /// <returns></returns>
    public static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Local URLs start with '/' but not '//' or '/\'
        if (url[0] == '/')
        {
            return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
        }

        // Local URLs can also start with '~/'
        return url.StartsWith("~/");
    }
}
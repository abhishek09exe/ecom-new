using System.Globalization;
using System.Net;
using ecom_new_api.Models.Requests;

namespace ecom_new_api.Services.MultiTrial;

public static class MultiTrialEmailRenderer
{
    public static IReadOnlyList<string> Render(MultiTrialSubmissionRequest request, string keycode,
        Guid licenseKey, int seats, IReadOnlyList<string> categories)
    {
        if (!categories.Contains("SAEP", StringComparer.OrdinalIgnoreCase) ||
            categories.Any(category => !new[] { "SAEP", "SDNS", "SECA" }.Contains(category, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Confirmation templates support only SAEP, SDNS and SECA profiles.");

        var urlBase = request.LocationCode.ToLowerInvariant() + "/" + request.LanguageCode.ToLowerInvariant();
        var values = new Dictionary<string, string>
        {
            ["{{first_name}}"] = WebUtility.HtmlEncode(request.FirstName ?? string.Empty),
            ["{{last_name}}"] = WebUtility.HtmlEncode(request.LastName ?? string.Empty),
            ["{{customer_email}}"] = WebUtility.HtmlEncode(request.CustomerEmail ?? string.Empty),
            ["{{keycode}}"] = WebUtility.HtmlEncode(keycode),
            ["{{license_key}}"] = licenseKey.ToString("D"),
            ["{{license_seats}}"] = seats.ToString(CultureInfo.InvariantCulture),
            ["{{url_base}}"] = WebUtility.HtmlEncode(urlBase)
        };
        var bodies = new List<string>();
        foreach (var category in categories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var body = category.ToUpperInvariant() switch
            {
                "SAEP" => Primary,
                "SDNS" => Module.Replace("{{product}}", "DNS Protection", StringComparison.Ordinal)
                    .Replace("{{category}}", "SDNS", StringComparison.Ordinal)
                    .Replace("{{guide_link_text}}", "DNS Protection Getting Started Guide", StringComparison.Ordinal)
                    .Replace("{{guide_link_punctuation}}", ".", StringComparison.Ordinal)
                    .Replace("{{guide}}", "https://docs.webroot.com/us/en/business/dnsprotection_gettingstartedguide/dnsprotection_gettingstartedguide.htm?rc=5346&utm_source=sfmc&utm_medium=email&utm_campaign=amer-smb-onln-endpoint-receipt", StringComparison.Ordinal),
                "SECA" => Module.Replace("{{product}}", "Security Awareness Training", StringComparison.Ordinal)
                    .Replace("{{category}}", "SECA", StringComparison.Ordinal)
                    .Replace("{{guide_link_text}}", "Security Awareness Training Getting Started Guide.", StringComparison.Ordinal)
                    .Replace("{{guide_link_punctuation}}", string.Empty, StringComparison.Ordinal)
                    .Replace("{{guide}}", "https://download.webroot.com/SecurityAwarenessTraining_GettingStartedGuide.pdf", StringComparison.Ordinal),
                _ => throw new InvalidOperationException("Unsupported trial email template.")
            };
            foreach (var value in values)
                body = body.Replace(value.Key, value.Value, StringComparison.Ordinal);
            var moduleHeader = categories.Any(category => category.Equals("SDNS", StringComparison.OrdinalIgnoreCase) ||
                category.Equals("SECA", StringComparison.OrdinalIgnoreCase));
            var logo = moduleHeader
                ? "https://www-cdn.webroot.com/5015/7851/9589/Webroot-opentext-logogreen-250x50.png"
                : "https://image.service-webroot.com/lib/fe9c12707465057c7c/m/5/16ab8b8d-8586-46e5-9a81-7124a232069f.png";
            var headerUrl = moduleHeader
                ? "https://www.webroot.com/us/en?utm_campaign=amer-csmr-smb-receipts&amp;utm_medium=email&amp;utm_source=sfmc"
                : "https://www.webroot.com/us/en/?utm_source=webroot&amp;utm_medium=reciept&amp;utm_term=homepage&amp;utm_campaign=webroot-reciept";
            bodies.Add(Layout
                .Replace("{{logo}}", logo, StringComparison.Ordinal)
                .Replace("{{header_url}}", headerUrl, StringComparison.Ordinal)
                .Replace("{{body}}", body, StringComparison.Ordinal));
        }
        return bodies;
    }

    private const string Layout = """
        <!DOCTYPE html><html lang="en"><head><meta charset="UTF-8" /></head>
        <body style="margin: 30px 0 0 0; padding: 0; font-family:Arial, sans-serif;">
        <table cellpadding="0" cellspacing="0" border="0" align="center" style="padding: 0; width:690px;">
        <tr><td align="left"><a rel="home" hreflang="en-US" href="{{header_url}}"><img alt="Webroot Antispyware, Virus and Malware Protection" src="{{logo}}"></a></td></tr></table>
        <table cellpadding="0" cellspacing="0" border="0" align="center" style="padding: 0; width:650px;"><tr><td align="left" style="color: #222F39;">{{body}}</td></tr></table>
        </body></html>
        """;

    private const string Primary = """
        <h1 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif; margin-top: 35px;">Your free trial instructions for Webroot<sup>&reg;</sup> Business Endpoint Protection</h1>
        <p>Dear {{first_name}} {{last_name}},</p>
        <p>Thank you for trialing Webroot<sup>&reg;</sup> Business Endpoint Protection. Your trial includes access to the Webroot business management console. This online console is where you’ll manage your endpoint security. All devices that receive the Endpoint Protection agent will automatically report to the management console.<br><br>
        Your trial is registered to: {{customer_email}}<br><br>Your keycode is: {{keycode}}<br><br>Follow the instructions below to get started.<br><br></p>
        <h3 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif">Getting Started</h3>
        <p>We’ll take you through these two major step to get you up and running.<br><br></p>
        <p><span style="font-weight: bold;">Step 1:</span> Registering for a trial. You have already completed this step!<br><br>
        <span style="font-weight: bold;">Steps 2:</span> The best way to install your Webroot<sup>&reg;</sup> Business Endpoint Protection trial is to follow the detailed steps in our <a href="https://docs.webroot.com/us/en/business/admingettingstartedguide/admingettingstartedguide.htm" style="font-weight: bold;">Getting Started Guide</a>.<br><br></p>
        <h3 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif">More Resources</h3>
        <p>During your trial, you have full access to our range of customer support resources. To get technical support, please submit a ticket <a href="https://www.webrootanywhere.com/servicewelcome.asp">here</a>, or visit our <a href="https://www.webroot.com/us/en/business/support?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">support center</a>.</p>
        <p>To get the most out of your trial, please visit our <a href="https://www.webroot.com/us/en/business/customer-success?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">Customer Success Hub</a>.</p>
        <p>We’ve also created the best collection of shared knowledge on internet security. Join us on our <a href="https://community.webroot.com/?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">Business Community</a> to connect with Webroot and industry peers.<br><br></p>
        <p style="font-size:1em;">Thank you,<br>The Webroot Team<br><span style="font-size:0.9em;">Webroot LLC<br>385 Interlocken Crescent, Suite 800<br>Broomfield, CO 80021<br>United States<br></span></p>
        <p style="font-size:0.9em;"><a href="https://www.webroot.com/{{url_base}}/legal/licensing-definition-guidelines">Licensing</a> | <a href="https://www.webroot.com/{{url_base}}/legal/privacy">Privacy Policy</a> | <a href="https://www.webroot.com/{{url_base}}/legal/licensing-tax-information#sales-tax">Sales Tax Information</a></p>
        """;

    private const string Module = """
        <h1 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif; margin-top: 35px;">Webroot<sup>&reg;</sup> {{product}} Trial Instructions</h1>
        <p>Dear Customer,</p><p>Thank you for choosing to try out Webroot<sup>&reg;</sup> {{product}}.<br>We have added the {{product}} service to your existing Webroot<sup>&reg;</sup> Business Endpoint Protection license.</p>
        <p><strong>Your keycode is: {{keycode}}</strong></p>
        <h3 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif">About Your Trial</h3>
        <p>Your trial use of {{product}} is governed by Webroot's <a href="https://www.webroot.com/us/en/legal/beta-service-terms-and-conditions">Beta Agreement</a>. By using {{product}}, you agree to and accept the terms of that agreement. {{product}} trials are for 30 days. {{product}} is being added to your existing Endpoint license. If you have separately received a trial Endpoint license that has less than 30 days remaining, your {{product}} trial will expire on the corresponding end date of your Endpoint license. If you wish to purchase a subscription to the production version of {{product}}, you will need to pay the applicable fees and enter into the applicable terms with Webroot.</p>
        <h3 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif">Next Steps</h3>
        <p>You should now see the additonal {{product}} functionality in your current Endpoint console. The {{product}} functionaility can now be deployed to your sites.</p>
        <p>If you need additional help, check out our <a href="{{guide}}">{{guide_link_text}}</a>{{guide_link_punctuation}}</p>
        <h3>Are You Fully Protected?</h3>
        <p>Webroot&reg; next-gen business cybersecurity solutions are effective, efficient, and easy to use. Best of all, they are <a href="https://www.webroot.com/us/en/business/integrated-solutions?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-endpoint-receipt">fully integrated</a> to work together to close the gaps in your (or your clients’) security strategy with the following products:</p>
        <ol><li><a href="https://www.webroot.com/us/en/business/smb/endpoint-protection?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-endpoint-receipt">Webroot<sup>&reg;</sup> Business Endpoint Protection</a></li>
        <li><a href="https://www.webroot.com/us/en/business/dns-protection?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-endpoint-receipt">Webroot<sup>&reg;</sup> DNS Protection</a></li>
        <li><a href="https://www.webroot.com/us/en/business/security-awareness?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-endpoint-receipt">Webroot<sup>&reg;</sup> Security Awareness Training</a></li></ol>
        <h3 style="font-family: 'Titillium Web', Helvetica, Arial, sans-serif">Ready to Buy?</h3>
        <p>Webroot is here to help secure your business and users across all the stages of a cyberattack.</p>
        <p><a style="font-family: 'Titillium Web', Helvetica, Arial, sans-serif; text-decoration: none; display:inline-block; background-color: #ff6b0b; color: #ffffff; text-align: center; line-height: 1; text-transform: uppercase; padding: 1em .8em; min-width: 8em; cursor: pointer; border-radius: 3px" href="https://www.webroot.com/us/en/cart/update?items[0][message_key]={{license_key}}&amp;items[0][license_category_name]={{category}}&amp;items[0][license_seats]={{license_seats}}&amp;items[0][item_hierarchy_id]=2&amp;routing_action=billing&amp;utm_medium=email&amp;utm_source=dns_seca_trial&amp;utm_campaign=transactional&amp;rc=5346&amp;ac=2000&amp;emcid=2000">Buy Now</a></p>
        <p>Call us at 1-800-870-8102, our representatives are at your service.</p>
        <h3 style="font-family: 'Roboto Condensed Bold', Helvetica, Arial, sans-serif">More Resources</h3>
        <p>During your trial, you have full access to our range of customer support resources. To get technical support, please submit a ticket <a href="https://www.webrootanywhere.com/servicewelcome.asp">here</a>, or visit our <a href="https://www.webroot.com/us/en/business/support?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">support center</a>.</p>
        <p>To get the most out of your trial, please visit our <a href="https://www.webroot.com/us/en/business/customer-success?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">Customer Success Hub</a>.</p>
        <p>We’ve also created the best collection of shared knowledge on internet security. Join us on our <a href="https://community.webroot.com/?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-trial-instructions">Business Community</a> to connect with Webroot and industry peers.<br></p>
        <p>Thank you,<br>The Webroot Team<br>Webroot, LLC<br>385 Interlocken Crescent, Suite 800<br>Broomfield CO 80021<br>United States<br><br><br>
        <a href="https://www.webroot.com/us/en/legal/licensing-definition-guidelines?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-receipt">Licensing</a> | <a href="https://www.webroot.com/us/en/legal/privacy?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-receipt">Privacy Policy</a> | <a href="https://www.webroot.com/us/en/legal/licensing-tax-information?rc=5346&amp;utm_source=sfmc&amp;utm_medium=email&amp;utm_campaign=amer-smb-onln-receipt">Sales Tax Information</a><br><br></p>
        """;
}
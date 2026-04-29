using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Collaborative_Task_Management_System.TagHelpers
{
    /// <summary>
    /// A Tag Helper that renders a DateTime value as a span element with a data-utc attribute.
    /// The frontend JavaScript will automatically convert the UTC time to the user's local timezone.
    /// 
    /// Usage in Razor views:
    ///   <local-time date="@Model.CreatedAt"></local-time>
    ///   <local-time date="@Model.CreatedAt" format="date"></local-time>
    ///   <local-time date="@Model.CreatedAt" format="datetime"></local-time>
    /// </summary>
    [HtmlTargetElement("local-time")]
    public class LocalTimeTagHelper : TagHelper
    {
        /// <summary>
        /// The DateTime value to display. Will be converted to UTC for the data attribute.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// The display format: "date" for date-only, "datetime" for date+time (default).
        /// </summary>
        public string Format { get; set; } = "datetime";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";

            if (Date.HasValue)
            {
                var utcDate = Date.Value.Kind == DateTimeKind.Utc
                    ? Date.Value
                    : DateTime.SpecifyKind(Date.Value, DateTimeKind.Utc);

                output.Attributes.SetAttribute("class", "utc-time");
                output.Attributes.SetAttribute("data-utc", utcDate.ToString("o"));
                output.Attributes.SetAttribute("data-format", Format);

                // Server-side fallback text (shown before JS runs or if JS fails)
                string fallbackText = Format switch
                {
                    "date" => utcDate.ToString("MMM dd, yyyy"),
                    _ => utcDate.ToString("MMM dd, yyyy HH:mm")
                };

                output.Content.SetContent(fallbackText);
            }
            else
            {
                output.Content.SetContent("N/A");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// JSON for what ZimAI.exe sends: dictionaries, strings, numbers, booleans and null.
static class Json {
    public static string Of(object value) {
        if (value == null) return "null";
        if (value is bool) return (bool)value ? "true" : "false";
        if (value is string) {
            var s = new StringBuilder("\"");
            foreach (char c in (string)value) {
                if (c == '"' || c == '\\') s.Append('\\').Append(c);
                else if (c < ' ') s.AppendFormat("\\u{0:x4}", (int)c);
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }
        var map = value as IDictionary<string, object>;
        if (map != null) return "{" + string.Join(",", map.Select(p => Of(p.Key) + ":" + Of(p.Value))) + "}";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}

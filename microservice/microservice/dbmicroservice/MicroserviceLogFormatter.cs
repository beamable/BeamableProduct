// // Decompiled with JetBrains decompiler
// // Type: Serilog.Formatting.Compact.RenderedCompactJsonFormatter
// // Assembly: Serilog.Formatting.Compact, Version=1.1.0.0, Culture=neutral, PublicKeyToken=24c2f752a8e58a10
// // MVID: 6DD69EB4-5D89-4DFE-9F10-03F7045F686F
// // Assembly location: /Users/chris/.nuget/packages/serilog.formatting.compact/1.1.0/lib/netstandard2.0/Serilog.Formatting.Compact.dll
//
// using Serilog.Events;
// using Serilog.Formatting.Json;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using Serilog.Formatting;
// using Serilog.Formatting.Compact;
//
// namespace Beamable.Server
// {
//   /// <summary>
//   /// Taken from the compact renderer; and adapted to work without @ symbols
//   /// An <see cref="T:Serilog.Formatting.ITextFormatter" /> that writes events in a compact JSON format, for consumption in environments
//   /// without message template support. Message templates are rendered into text and a hashed event id is included.
//   /// </summary>
//   public class MicroserviceLogFormatter : ITextFormatter
//   {
//     private readonly JsonValueFormatter _valueFormatter;
//
//     public MicroserviceLogFormatter(JsonValueFormatter valueFormatter = null)
//     {
//       this._valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");
//     }
//
//     /// <summary>
//     /// Format the log event into the output. Subsequent events will be newline-delimited.
//     /// </summary>
//     /// <param name="logEvent">The event to format.</param>
//     /// <param name="output">The output.</param>
//     public void Format(LogEvent logEvent, TextWriter output)
//     {
//       FormatEvent(logEvent, output, this._valueFormatter);
//       output.WriteLine();
//     }
//
//     /// <summary>Format the log event into the output.</summary>
//     /// <param name="logEvent">The event to format.</param>
//     /// <param name="output">The output.</param>
//     /// <param name="valueFormatter">A value formatter for <see cref="T:Serilog.Events.LogEventPropertyValue" />s on the event.</param>
//     public static void FormatEvent(
//       LogEvent logEvent,
//       TextWriter output,
//       JsonValueFormatter valueFormatter)
//     {
//       if (logEvent == null)
//         throw new ArgumentNullException(nameof (logEvent));
//       if (output == null)
//         throw new ArgumentNullException(nameof (output));
//       if (valueFormatter == null)
//         throw new ArgumentNullException(nameof (valueFormatter));
//       output.Write("{\"__t\":\"");
//       output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
//       output.Write("\",\"__m\":");
//       JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Render(logEvent.Properties, (IFormatProvider) null), output);
//       if (logEvent.Level != LogEventLevel.Information)
//       {
//         output.Write(",\"__l\":\"");
//         output.Write((object) logEvent.Level);
//         output.Write('"');
//       }
//       else
//       {
//         output.Write(",\"__l\":\"");
//         output.Write("Info");
//         output.Write('"');
//       }
//       if (logEvent.Exception != null)
//       {
//         output.Write(",\"__x\":");
//         JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
//       }
//       foreach (KeyValuePair<string, LogEventPropertyValue> property in (IEnumerable<KeyValuePair<string, LogEventPropertyValue>>) logEvent.Properties)
//       {
//         string str = property.Key;
//         if (str.Length > 0 && str[0] == '@')
//           str = "__" + str;
//         output.Write(',');
//         JsonValueFormatter.WriteQuotedJsonString(str, output);
//         output.Write(':');
//         valueFormatter.Format(property.Value, output);
//       }
//       output.Write('}');
//     }
//   }
// }

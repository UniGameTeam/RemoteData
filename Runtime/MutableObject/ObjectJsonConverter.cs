namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ObjectJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            return DeserializeToDictionaryOrList(obj);
        }

        private static object DeserializeToDictionaryOrList(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var values = token.ToObject<Dictionary<string, object>>();
                foreach (KeyValuePair<string, object> d in values.ToList())
                {
                    if (d.Value is JObject)
                    {
                        values[d.Key] = DeserializeToDictionaryOrList(d.Value as  JToken);
                    }
                    else if (d.Value is JArray)
                    {
                        values[d.Key] = DeserializeToDictionaryOrList(d.Value as JToken);
                    }
                }
                return values;
            }
            else if(token.Type == JTokenType.Array)
            {
                var values = token.ToObject<List<object>>();
                for (int i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    if (value is JObject)
                    {
                        values[i] = DeserializeToDictionaryOrList(value as JToken);
                    }
                    else if (value is JArray)
                    {
                        values[i] = DeserializeToDictionaryOrList(value as JToken);
                    }
                }

                return values;
            }
            else
            {
                return token.Value<string>();
            }
        }

        public override bool CanConvert(Type objectType) => throw new NotImplementedException();
    }
}
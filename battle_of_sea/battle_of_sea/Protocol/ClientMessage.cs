using System.Text.Json;

namespace battle_of_sea.Protocol
{
     class ClientMessage
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}

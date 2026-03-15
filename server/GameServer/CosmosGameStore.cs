using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamlands.Game;
using Microsoft.Azure.Cosmos;

namespace GameServer;

public class CosmosGameStore : IGameStore
{
    readonly Container _container;

    public CosmosGameStore(Container container) => _container = container;

    public async Task<PlayerState?> Load(string gameId)
    {
        try
        {
            var response = await _container.ReadItemAsync<GameDocument>(
                gameId, new PartitionKey(gameId));
            response.Resource.State.ConcurrencyToken = response.ETag;
            return response.Resource.State;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task Save(PlayerState state)
    {
        var doc = new GameDocument { Id = state.GameId, State = state };
        var options = new ItemRequestOptions();
        if (state.ConcurrencyToken != null)
            options.IfMatchEtag = state.ConcurrencyToken;

        var response = await _container.UpsertItemAsync(doc, new PartitionKey(doc.Id), options);
        state.ConcurrencyToken = response.ETag;
    }

    public static CosmosClient CreateClient(string connectionString)
    {
        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        return new CosmosClient(connectionString, new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = jsonOpts,
        });
    }

    class GameDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = "";
        public PlayerState State { get; init; } = null!;
    }
}

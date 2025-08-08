using Connect4Client.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Connect4Client.Services
{
    public class ApiService
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl = "http://localhost:5000";

        public ApiService()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<Player?> GetPlayerByPlayerId(int playerId)
        {
            try
            {
                var response = await httpClient.GetAsync($"/api/Players/byplayerid/{playerId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Player>(json);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error in GetPlayerByPlayerId: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdatePlayerStatistics(Player player)
        {
            try
            {
                var json = JsonConvert.SerializeObject(player);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PutAsync($"/api/Players/{player.Id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error in UpdatePlayerStatistics: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Player>> GetAllPlayers()
        {
            try
            {
                var response = await httpClient.GetAsync("/api/Players");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<Player>>(json) ?? new List<Player>();
                }
                
                return new List<Player>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error in GetAllPlayers: {ex.Message}");
                return new List<Player>();
            }
        }

        public async Task<StartGameResponse> StartGame(int playerId)
        {
            try
            {
                var request = new StartGameRequest { PlayerId = playerId };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("/api/Games/start", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                return JsonConvert.DeserializeObject<StartGameResponse>(responseJson) ?? new StartGameResponse
                {
                    Success = false,
                    Message = "Failed to deserialize response"
                };
            }
            catch (Exception ex)
            {
                return new StartGameResponse
                {
                    Success = false,
                    Message = $"Error starting game: {ex.Message}"
                };
            }
        }

        public async Task<MakeMoveResponse> MakeMove(int gameId, int column)
        {
            try
            {
                var request = new MakeMoveRequest { GameId = gameId, Column = column };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("/api/Games/move", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                return JsonConvert.DeserializeObject<MakeMoveResponse>(responseJson) ?? new MakeMoveResponse
                {
                    Success = false,
                    Message = "Failed to deserialize response"
                };
            }
            catch (Exception ex)
            {
                return new MakeMoveResponse
                {
                    Success = false,
                    Message = $"Error making move: {ex.Message}"
                };
            }
        }

        public async Task<GameDto?> GetGame(int gameId)
        {
            try
            {
                var response = await httpClient.GetAsync($"/api/Games/{gameId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GameDto>(json);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error in GetGame: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                var response = await httpClient.GetAsync("/api/Players");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
} 
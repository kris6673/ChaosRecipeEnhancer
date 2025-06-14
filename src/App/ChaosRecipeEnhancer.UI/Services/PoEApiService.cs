﻿using ChaosRecipeEnhancer.UI.Models.ApiResponses.OAuthEndpointResponses;
using ChaosRecipeEnhancer.UI.Models.ApiResponses.SessionIdEndpointResponses;
using ChaosRecipeEnhancer.UI.Models.ApiResponses.Shared;
using ChaosRecipeEnhancer.UI.Models.Config;
using ChaosRecipeEnhancer.UI.Models.Enums;
using ChaosRecipeEnhancer.UI.Models.Exceptions;
using ChaosRecipeEnhancer.UI.Models.UserSettings;
using ChaosRecipeEnhancer.UI.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChaosRecipeEnhancer.UI.Services;

/// <summary>
/// Defines the contract for the PoE API service.
/// </summary>
public interface IPoeApiService
{
    /// <summary>
    /// Gets a list of league asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of league names.</returns>
    public Task<List<string>> GetLeaguesAsync();

    /// <summary>
    /// Retrieves the metadata for all personal stash tabs asynchronously.
    /// </summary>
    /// <remarks>This API call counts towards the shared CRE rate limit.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of stashes response.</returns>
    public Task<List<UnifiedStashTabMetadata>> GetAllPersonalStashTabMetadataWithOAuthAsync();

    public Task<List<UnifiedStashTabMetadata>> GetAllGuildStashTabMetadataWithOAuthAsync();

    /// <summary>
    /// Retrieves the contents of a personal stash tab by its ID asynchronously.
    /// </summary>
    /// <remarks>This API call counts towards the shared CRE rate limit.</remarks>
    /// <param name="stashId">The ID of the stash tab.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the get stash response.</returns>
    public Task<UnifiedStashTabContents> GetPersonalStashTabContentsByStashIdWithOAuthAsync(string stashId);

    public Task<UnifiedStashTabContents> GetGuildStashTabContentsByStashIdWithOAuthAsync(string stashId);

    public Task<AccountAvatarResponse> HealthCheckWithSesionIdAsync(string sessionId);

    /// <summary>
    /// Retrieves the metadata for all personal  stash tabs asynchronously using session ID authentication.
    /// </summary>
    /// <remarks>This API call counts towards the shared PoE site rate limit (i.e. rate limits made with these calls apply 'globally').</remarks>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="leagueName">The name of the league.</param>
    /// <param name="secret">The session ID secret for authentication.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata for all guild stash tabs.</returns>
    public Task<List<UnifiedStashTabMetadata>> GetAllPersonalStashTabMetadataWithSessionIdAsync(string sessionId);

    /// <summary>
    /// Retrieves the metadata for all guild stash tabs asynchronously using session ID authentication.
    /// </summary>
    /// <remarks>This API call counts towards the shared PoE site rate limit (i.e. rate limits made with these calls apply 'globally').</remarks>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="leagueName">The name of the league.</param>
    /// <param name="secret">The session ID secret for authentication.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata for all guild stash tabs.</returns>
    public Task<List<UnifiedStashTabMetadata>> GetAllGuildStashTabMetadataWithSessionIdAsync(string sessionId);

    /// <summary>
    /// Retrieves the contents of a personal stash tab by its index asynchronously using session ID authentication.
    /// </summary>
    /// <remarks>This API call counts towards the shared PoE site rate limit (i.e. rate limits made with these calls apply 'globally').</remarks>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="leagueName">The name of the league.</param>
    /// <param name="tabIndex">The index of the stash tab.</param>
    /// <param name="secret">The session ID secret for authentication.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the contents of the specified personal stash tab.</returns>
    public Task<UnifiedStashTabContents> GetPersonalStashTabContentsByIndexWithSessionIdAsync(string sessionId, string tabId, string tabName, int tabIndex, string tabType);

    /// <summary>
    /// Retrieves the contents of a guild stash tab by its index asynchronously using session ID authentication.
    /// </summary>
    /// <remarks>This API call counts towards the shared PoE site rate limit (i.e. rate limits made with these calls apply 'globally').</remarks>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="leagueName">The name of the league.</param>
    /// <param name="tabIndex">The index of the stash tab.</param>
    /// <param name="secret">The session ID secret for authentication.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the contents of the specified guild stash tab.</returns>
    public Task<UnifiedStashTabContents> GetGuildStashTabContentsByIndexWithSessionIdAsync(string sessionId, string tabId, string tabName, int tabIndex, string tabType);
}

/// <summary>
/// Provides methods to interact with the Path of Exile API.
/// </summary>
public class PoeApiService : IPoeApiService
{
    #region Fields

    private readonly ILogger _log = Log.ForContext<PoeApiService>();
    private readonly IUserSettings _userSettings;
    private readonly IAuthStateManager _authStateManager;
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PoeApiService"/> class.
    /// </summary>
    /// <param name="userSettings">The user settings.</param>
    /// <param name="authStateManager">The authentication state manager.</param>
    public PoeApiService(IHttpClientFactory httpClientFactory, IUserSettings userSettings, IAuthStateManager authStateManager)
    {
        _httpClientFactory = httpClientFactory;
        _userSettings = userSettings;
        _authStateManager = authStateManager;
    }

    #endregion

    #region Properties

    public bool CustomLeagueEnabled => _userSettings.CustomLeagueEnabled;

    #endregion

    #region Domain Methods

    /// <inheritdoc />
    public async Task<List<string>> GetLeaguesAsync()
    {
        List<string> leagueNames;

        if (CustomLeagueEnabled)
        {
            var results = await GetPersonalLeaguesAsync();

            if (results is null) return [];

            leagueNames = results.Leagues
                .Where(league => !string.IsNullOrEmpty(league.PrivateLeagueUrl))
                .Select(league => league.Id)
                .ToList();
        }
        else
        {
            var results = await GetPublicLeaguesAsync();
            leagueNames = results.Select(league => league.Id).ToList();
        }

        return leagueNames;
    }

    /// <summary>
    /// Get a list of public leagues asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of public leagues.</returns>
    private async Task<IEnumerable<League>> GetPublicLeaguesAsync()
    {
        var responseRaw = await GetAsync(PoeApiConfig.PublicLeagueEndpoint);

        if (responseRaw is null) return [];

        return JsonSerializer.Deserialize<League[]>((string)responseRaw);
    }

    /// <summary>
    /// Get a list of personal leagues asynchronously.
    /// </summary>
    /// <remarks>This API call counts towards the shared CRE rate limit.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of personal leagues.</returns>
    private async Task<LeagueResponse> GetPersonalLeaguesAsync()
    {
        var responseRaw = await GetAuthenticatedWithOAuthAsync(PoeApiConfig.PersonalLeaguesOAuthEndpoint());

        if (responseRaw is null) return null;

        return JsonSerializer.Deserialize<LeagueResponse>((string)responseRaw);
    }

    /// <inheritdoc />
    public async Task<List<UnifiedStashTabMetadata>> GetAllPersonalStashTabMetadataWithOAuthAsync()
    {
        var response = await GetAuthenticatedWithOAuthAsync(PoeApiConfig.PersonalStashTabPropsOAuthEndpoint());

        if (response is null) return [];

        var listStashesResponse = JsonSerializer.Deserialize<ListStashesResponse>((string)response);
        return listStashesResponse.ToUnifiedMetadata();
    }

    public async Task<List<UnifiedStashTabMetadata>> GetAllGuildStashTabMetadataWithOAuthAsync()
    {
        var response = await GetAuthenticatedWithOAuthAsync(PoeApiConfig.GuildStashTabPropsOAuthEndpoint());

        if (response is null) return [];

        var listStashesResponse = JsonSerializer.Deserialize<ListStashesResponse>((string)response);
        return listStashesResponse.ToUnifiedMetadata();
    }

    /// <inheritdoc />
    public async Task<UnifiedStashTabContents> GetPersonalStashTabContentsByStashIdWithOAuthAsync(string stashId)
    {
        var responseRaw = await GetAuthenticatedWithOAuthAsync(
            PoeApiConfig.PersonalIndividualTabContentsOAuthEndpoint(stashId)
        );

        if (responseRaw is null) return null;

        var response = JsonSerializer.Deserialize<GetStashResponse>((string)responseRaw);
        return response?.ToUnifiedContents();
    }

    public async Task<UnifiedStashTabContents> GetGuildStashTabContentsByStashIdWithOAuthAsync(string stashId)
    {
        var responseRaw = await GetAuthenticatedWithOAuthAsync(
            PoeApiConfig.GuildIndividualTabContentsOAuthEndpoint(stashId)
        );

        if (responseRaw is null) return null;

        var response = JsonSerializer.Deserialize<GetStashResponse>((string)responseRaw);
        return response?.ToUnifiedContents();
    }

    public async Task<AccountAvatarResponse> HealthCheckWithSesionIdAsync(string sessionId)
    {
        var response = await GetAuthenticatedWithSessionIdForHealthcheckAsync(
            PoeApiConfig.HealthCheckSessionIdEndpoint(),
            sessionId
        );

        // Check if the response is an error
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>((string)response);
        if (errorResponse?.Error != null && errorResponse.Error.Code == 8)
        {
            throw new UnauthorizedException(errorResponse.Error.Message);
        }

        return JsonSerializer.Deserialize<AccountAvatarResponse>((string)response);
    }

    /// <inheritdoc />
    public async Task<List<UnifiedStashTabMetadata>> GetAllPersonalStashTabMetadataWithSessionIdAsync(string sessionId)
    {
        var response = await GetAuthenticatedWithSessionIdAsync(
            PoeApiConfig.StashTabPropsSessionIdEndpoint(TargetStash.Personal),
            sessionId
        );

        if (response is null) return [];

        var baseStashTabMetadataList = JsonSerializer.Deserialize<BaseStashTabMetadataList>((string)response);
        return baseStashTabMetadataList.ToUnifiedMetadata();
    }

    /// <inheritdoc />
    public async Task<List<UnifiedStashTabMetadata>> GetAllGuildStashTabMetadataWithSessionIdAsync(string sessionId)
    {
        _log.Information($"[GetAllGuildStashTabMetadataWithSessionIdAsync] Called with sessionId: {{masked}} (length: {sessionId?.Length})");
        var response = await GetAuthenticatedWithSessionIdAsync(
            PoeApiConfig.StashTabPropsSessionIdEndpoint(TargetStash.Guild),
            sessionId
        );

        if (response is null)
        {
            _log.Warning("[GetAllGuildStashTabMetadataWithSessionIdAsync] Response is null.");
            return [];
        }

        var baseStashTabMetadataList = JsonSerializer.Deserialize<BaseStashTabMetadataList>((string)response);
        var unified = baseStashTabMetadataList.ToUnifiedMetadata();
        _log.Information($"[GetAllGuildStashTabMetadataWithSessionIdAsync] Returning {unified?.Count} tabs.");
        return unified;
    }

    /// <inheritdoc />
    public async Task<UnifiedStashTabContents> GetPersonalStashTabContentsByIndexWithSessionIdAsync(string sessionId, string tabId, string tabName, int tabIndex, string tabType)
    {
        _log.Information($"[GetPersonalStashTabContentsByIndexWithSessionIdAsync] Called with sessionId: {{masked}}, tabId: {tabId}, tabName: {tabName}, tabIndex: {tabIndex}, tabType: {tabType}");
        var responseRaw = await GetAuthenticatedWithSessionIdAsync(
            PoeApiConfig.IndividualTabContentsSessionIdEndpoint(TargetStash.Personal, tabIndex),
            sessionId
        );

        if (responseRaw is null)
        {
            _log.Warning("[GetPersonalStashTabContentsByIndexWithSessionIdAsync] Response is null.");
            return null;
        }

        var response = JsonSerializer.Deserialize<BaseStashTabContents>((string)responseRaw);
        var unified = response?.ToUnifiedContents(tabId, tabName, tabIndex, tabType);
        _log.Information($"[GetPersonalStashTabContentsByIndexWithSessionIdAsync] Returning contents for tabId: {tabId}, items: {unified?.Items?.Count}");
        return unified;
    }

    /// <inheritdoc />
    public async Task<UnifiedStashTabContents> GetGuildStashTabContentsByIndexWithSessionIdAsync(string sessionId, string tabId, string tabName, int tabIndex, string tabType)
    {
        var responseRaw = await GetAuthenticatedWithSessionIdAsync(
            PoeApiConfig.IndividualTabContentsSessionIdEndpoint(TargetStash.Guild, tabIndex),
            sessionId
        );

        if (responseRaw is null) return null;

        var response = JsonSerializer.Deserialize<BaseStashTabContents>((string)responseRaw);
        return response?.ToUnifiedContents(tabId, tabName, tabIndex, tabType);
    }

    #endregion

    #region Private Utility Methods

    /// <summary>
    /// Sends a GET request to the specified URI using OAuth.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content.</returns>
    private async Task<object> GetAuthenticatedWithOAuthAsync(Uri requestUri)
    {
        if (GlobalRateLimitState.CheckForBan()) return null;

        // -1 for 1 request + 3 times if rate limit high exceeded
        if (GlobalRateLimitState.RateLimitState[0] >= GlobalRateLimitState.MaximumRequests - 4)
        {
            GlobalRateLimitState.RateLimitExceeded = true;
            return null;
        }

        // create new http client that will be disposed of after request
        var client = _httpClientFactory.CreateClient(PoeApiConfig.PoeApiHttpClientName);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(PoeApiConfig.UserAgent);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authStateManager.AuthToken);

        // send request
        var response = await client.GetAsync(requestUri);
        var responseString = response.Content.ReadAsStringAsync().Result;

        _log.Information($"Fetch Result {requestUri}: {response.StatusCode}");
        _log.Information($"Response: {responseString}");

        // for some weird ass reason the status codes come
        // back 200 even when it's not valid (for leagues endpoint)
        // so here's a hacky work-around
        // HACK: GGG Fix ur shit
        if (response.StatusCode == HttpStatusCode.OK && !_authStateManager.ValidateAuthToken())
        {
            _log.Information("Status code is 200 but auth token is no good; manually replacing status code");
            response.StatusCode = HttpStatusCode.Unauthorized;
        }

        if (!CheckIfResponseStatusCodeIsValid(response, responseString)) return null;

        // get new rate limit values
        // these might end up being:
        //
        //      `X-Rate-Limit-Ip`
        //      `X-Rate-Limit-Ip-State`
        //      `X-Rate-Limit-Client`
        //      `X-Rate-Limit-Client-State`
        //
        // keep an eye on this if you get some weird issues...
        HttpHeaderUtilities.ProcessRateLimitHeaders(response, _log, requestUri);

        using var resultHttpContent = response.Content;

        var resultString = await resultHttpContent.ReadAsStringAsync();

        return resultString;
    }

    /// <summary>
    /// Sends a GET request to the specified URI using an included Session ID auth token.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content.</returns>
    private async Task<object> GetAuthenticatedWithSessionIdAsync(Uri requestUri, string sessionId)
    {
        _log.Information($"[GetAuthenticatedWithSessionIdAsync] Called with URI: {requestUri}, sessionId: {{masked}} (length: {sessionId?.Length})");
        _log.Information($"[GetAuthenticatedWithSessionIdAsync] Full request URI: {requestUri.AbsoluteUri}");
        _log.Information($"[GetAuthenticatedWithSessionIdAsync] Request sessionId (first 4 chars): {sessionId?.Substring(0, Math.Min(4, sessionId.Length))}... (length: {sessionId?.Length})");
        if (GlobalRateLimitState.CheckForBan()) {
            _log.Warning("[GetAuthenticatedWithSessionIdAsync] Global ban detected.");
            return null;
        }

        if (GlobalRateLimitState.RateLimitState[0] >= GlobalRateLimitState.MaximumRequests - 4)
        {
            GlobalRateLimitState.RateLimitExceeded = true;
            _log.Warning("[GetAuthenticatedWithSessionIdAsync] Rate limit exceeded.");
            return null;
        }

        var cookieContainer = new CookieContainer();
        cookieContainer.Add(requestUri, new Cookie("POESESSID", sessionId));
        _log.Information($"[GetAuthenticatedWithSessionIdAsync] Cookies: {string.Join(", ", cookieContainer.GetCookies(requestUri).Cast<Cookie>().Select(c => c.Name + "=" + c.Value))}");
        using var handler = new HttpClientHandler();
        handler.CookieContainer = cookieContainer;

        using var client = new HttpClient(handler);

        // add user agent
        var userAgent = $"CRE/v{Assembly.GetExecutingAssembly().GetName().Version}";
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        _log.Information($"[GetAuthenticatedWithSessionIdAsync] User-Agent: {userAgent}");

        // log all headers
        foreach (var header in client.DefaultRequestHeaders)
        {
            _log.Information($"[GetAuthenticatedWithSessionIdAsync] Header: {header.Key} = {string.Join(",", header.Value)}");
        }

        // send request
        var response = await client.GetAsync(requestUri);
        var responseString = await response.Content.ReadAsStringAsync();

        _log.Information($"[GetAuthenticatedWithSessionIdAsync] Fetch Result {requestUri}: {response.StatusCode}");
        _log.Debug($"[GetAuthenticatedWithSessionIdAsync] Response: {responseString}");

        if (!response.IsSuccessStatusCode)
        {
            _log.Warning($"[GetAuthenticatedWithSessionIdAsync] Non-success status code: {response.StatusCode}");
            return null;
        }

        if (!CheckIfResponseStatusCodeIsValid(response, responseString)) {
            _log.Warning($"[GetAuthenticatedWithSessionIdAsync] Status code not valid: {response.StatusCode}");
            return null;
        }

        HttpHeaderUtilities.ProcessRateLimitHeaders(response, _log, requestUri);

        return responseString;
    }

    private async Task<object> GetAuthenticatedWithSessionIdForHealthcheckAsync(Uri requestUri, string sessionId)
    {
        if (GlobalRateLimitState.CheckForBan()) return null;

        if (GlobalRateLimitState.RateLimitState[0] >= GlobalRateLimitState.MaximumRequests - 4)
        {
            GlobalRateLimitState.RateLimitExceeded = true;
            return null;
        }

        _log.Information($"Session ID: {sessionId}");

        var cookieContainer = new CookieContainer();
        cookieContainer.Add(requestUri, new Cookie("POESESSID", sessionId));
        using var handler = new HttpClientHandler();
        handler.CookieContainer = cookieContainer;

        using var client = new HttpClient(handler);

        // add user agent
        client.DefaultRequestHeaders.Add("User-Agent", $"CRE/v{Assembly.GetExecutingAssembly().GetName().Version}");

        // send request
        var response = await client.GetAsync(requestUri);
        var responseString = await response.Content.ReadAsStringAsync();

        _log.Information($"Fetch Result {requestUri}: {response.StatusCode}");
        _log.Information($"Response: {responseString}");

        return responseString;
    }

    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content.</returns>
    private async Task<object> GetAsync(Uri requestUri)
    {
        if (GlobalRateLimitState.CheckForBan()) return null;

        // -1 for 1 request + 3 times if rate limit high exceeded
        if (GlobalRateLimitState.RateLimitState[0] >= GlobalRateLimitState.MaximumRequests - 4)
        {
            GlobalRateLimitState.RateLimitExceeded = true;
            return null;
        }

        // create new http client that will be disposed of after request
        using var client = new HttpClient();

        // as of some point between 3.24 and 3.25, this is now a required field so definitely include it!
        // ty to Novynn for ur help ur a g
        client.DefaultRequestHeaders.UserAgent.ParseAdd(PoeApiConfig.UserAgent);

        var response = await client.GetAsync(requestUri);
        var responseString = response.Content.ReadAsStringAsync().Result;

        _log.Information($"Fetch Result {requestUri}: {response.StatusCode}");
        _log.Information($"Response: {responseString}");

        if (!CheckIfResponseStatusCodeIsValid(response, responseString)) return null;

        return responseString;
    }

    private bool CheckIfResponseStatusCodeIsValid(HttpResponseMessage response, string responseString)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Forbidden:
                GlobalErrorHandler.HandleError403FromApi(responseString);

                // usually we will be here if we weren't able to make a successful api request based on an expired auth token
                _authStateManager.Logout();

                return false;

            case HttpStatusCode.Unauthorized:
                GlobalErrorHandler.HandleError401FromApi(responseString);

                // if we're here, the auth token is invalid
                // so we need to log out and reset auth state
                _authStateManager.Logout();

                return false;

            case HttpStatusCode.TooManyRequests:
                var retryAfterSeconds = GlobalErrorHandler.HandleError429FromApi(response, responseString);
                throw new RateLimitException(retryAfterSeconds);

            case HttpStatusCode.InternalServerError:
                GlobalErrorHandler.HandleError500FromApi(responseString);
                return false;

            case HttpStatusCode.ServiceUnavailable:
                GlobalErrorHandler.HandleError503FromApi(responseString);
                return false;

            default:

                // handle any other 4xx or 5XX errors
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    GlobalErrorHandler.HandleUnspecifiedErrorFromApi(responseString);
                    return false;
                }

                break;
        }

        return true;
    }

    #endregion
}
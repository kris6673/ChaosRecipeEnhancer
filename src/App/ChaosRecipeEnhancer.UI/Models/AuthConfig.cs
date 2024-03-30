﻿using ChaosRecipeEnhancer.UI.Models.Constants;
using System;

namespace ChaosRecipeEnhancer.UI.Models;

// Production Setup

public static class AuthConfig
{
    // REF: https://www.pathofexile.com/developer/docs/authorization#clients-public
    public static readonly int DefaultTokenExpirationHours = 10;

    // Production Config
    public static readonly Uri OAuthTokenEndpoint = new("https://chaos-recipe.com/auth/token");
    public static readonly string OAuthRedirectUri = "https://chaos-recipe.com/auth/success";

    // Sandbox Config
    //    public static readonly Uri OAuthTokenEndpoint = new("https://sandbox.chaos-recipe.com/auth/token");
    //    public static readonly string OAuthRedirectUri = "https://sandbox.chaos-recipe.com/auth/success";

    public static string CreClientVersionAuthParam => CreAppConstants.VersionText;

    // You can only request account:* scopes - NO service:* scopes
    public static readonly string Scopes = Uri.EscapeDataString("account:leagues account:stashes account:characters account:item_filter");

    public static string RedirectUri(string state, string codeVerifier, string creClientVersion)
    =>
        $"https://www.pathofexile.com/oauth/authorize?" +
        $"client_id=chaosrecipeenhancer&" +
        $"response_type=code&" +
        $"scope={Scopes}&" +
        $"state={Uri.EscapeDataString($"{state}|{creClientVersion}")}&" +
        $"redirect_uri={OAuthRedirectUri}&" +
        $"code_challenge={codeVerifier}&" +
        $"code_challenge_method=S256";
}
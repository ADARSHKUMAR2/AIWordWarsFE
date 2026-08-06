/*
 * Shop/Monetization Data Models
 * 
 * These models match the JSON responses from the monetization service
 * Used for deserializing API responses
 */

using System;
using System.Collections.Generic;

namespace WordWars.Models
{
    /// <summary>
    /// Response from purchase validation endpoint
    /// </summary>
    [Serializable]
    public class PurchaseValidationResponse
    {
        public bool success;
        public string product_name;
        public GrantedItems granted_items;
        public string transaction_id;
    }

    [Serializable]
    public class GrantedItems
    {
        public int hints;
        public bool remove_ads;
        public bool is_premium;
    }

    /// <summary>
    /// User's complete inventory
    /// </summary>
    [Serializable]
    public class InventoryResponse
    {
        public int hints;
        public bool remove_ads;
        public bool is_premium;
        public string premium_expires_at;  // ISO date string
        public List<string> owned_themes;
        public List<string> owned_avatars;
        public List<string> owned_titles;
        public string active_theme;
        public string active_avatar;
        public string active_title;
    }

    /// <summary>
    /// Battle Pass status
    /// </summary>
    [Serializable]
    public class BattlePassResponse
    {
        public int current_tier;
        public int total_xp;
        public int xp_for_next_tier;
        public bool is_premium;
        public List<int> claimed_free_tiers;
        public List<int> claimed_premium_tiers;
        public string season_start;  // ISO date string
        public string season_end;    // ISO date string
    }

    /// <summary>
    /// Response from claiming a battle pass reward
    /// </summary>
    [Serializable]
    public class ClaimRewardResponse
    {
        public bool success;
        public int tier;
        public string reward_type;
        public Dictionary<string, object> granted;
    }
}

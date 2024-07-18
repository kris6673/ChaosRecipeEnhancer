using ChaosRecipeEnhancer.UI.Models;
using ChaosRecipeEnhancer.UI.Models.ApiResponses.Shared;
using ChaosRecipeEnhancer.UI.Models.Exceptions;
using ChaosRecipeEnhancer.UI.Models.UserSettings;
using ChaosRecipeEnhancer.UI.Services;
using ChaosRecipeEnhancer.UI.Services.FilterManipulation;
using ChaosRecipeEnhancer.UI.UserControls;
using ChaosRecipeEnhancer.UI.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Enums = ChaosRecipeEnhancer.UI.Models.Enums;

namespace ChaosRecipeEnhancer.UI.Windows;

public sealed class SetTrackerOverlayViewModel : CreViewModelBase
{
    #region Fields

    private readonly IFilterManipulationService _filterManipulationService;
    private readonly IReloadFilterService _reloadFilterService;
    private readonly IPoeApiService _apiService;
    private readonly IUserSettings _userSettings;
    private readonly IAuthStateManager _authStateManager;
    private readonly INotificationSoundService _notificationSoundService;

    private const string SetsFullText = "Sets full!";

    // this should match the cooldown we apply in the log watcher manager
    /// <see cref="LogWatcherManager.AutoFetchCooldownSeconds"/>
    private const int FetchCooldownSeconds = 15;

    private bool _fetchButtonEnabled = true;
    private bool _stashButtonTooltipEnabled = false;
    private bool _setsTooltipEnabled = false;
    private string _warningMessage;

    #endregion

    #region Constructor

    public SetTrackerOverlayViewModel(
        IFilterManipulationService filterManipulationService,
        IReloadFilterService reloadFilterService,
        IPoeApiService apiService,
        IUserSettings userSettings,
        IAuthStateManager authStateManager,
        INotificationSoundService notificationSoundService
    )
    {
        _filterManipulationService = filterManipulationService;
        _reloadFilterService = reloadFilterService;
        _apiService = apiService;
        _userSettings = userSettings;
        _authStateManager = authStateManager;
        _notificationSoundService = notificationSoundService;
    }

    #endregion

    #region Properties

    #region User Settings Accessor Properties

    public Enums.SetTrackerOverlayItemCounterDisplayMode SetTrackerOverlayItemCounterDisplayMode
    {
        get => _userSettings.SetTrackerOverlayItemCounterDisplayMode;
        set
        {
            if (_userSettings.SetTrackerOverlayItemCounterDisplayMode != value)
            {
                _userSettings.SetTrackerOverlayItemCounterDisplayMode = value;
                OnPropertyChanged(nameof(SetTrackerOverlayItemCounterDisplayMode));
            }
        }
    }

    public int FullSetThreshold
    {
        get => _userSettings.FullSetThreshold;
        set
        {
            if (_userSettings.FullSetThreshold != value)
            {
                _userSettings.FullSetThreshold = value;
                OnPropertyChanged(nameof(FullSetThreshold));
            }
        }
    }

    public bool LegacyAuthMode
    {
        get => _userSettings.LegacyAuthMode;
        set
        {
            if (_userSettings.LegacyAuthMode != value)
            {
                _userSettings.LegacyAuthMode = value;
                OnPropertyChanged(nameof(LegacyAuthMode));
            }
        }
    }

    public string LegacyAuthSessionId
    {
        get => _userSettings.LegacyAuthSessionId;
        set
        {
            if (_userSettings.LegacyAuthSessionId != value)
            {
                _userSettings.LegacyAuthSessionId = value;
                OnPropertyChanged(nameof(LegacyAuthSessionId));
            }
        }
    }

    public bool GuildStashMode
    {
        get => _userSettings.GuildStashMode;
        set
        {
            if (_userSettings.GuildStashMode != value)
            {
                _userSettings.GuildStashMode = value;
                OnPropertyChanged(nameof(GuildStashMode));
            }
        }
    }

    public int StashTabQueryMode
    {
        get => _userSettings.StashTabQueryMode;
        set
        {
            if (_userSettings.StashTabQueryMode != value)
            {
                _userSettings.StashTabQueryMode = value;
                OnPropertyChanged(nameof(StashTabQueryMode));
            }
        }
    }

    public HashSet<string> StashTabIds
    {
        get => _userSettings.StashTabIds;
        set
        {
            if (_userSettings.StashTabIds != value)
            {
                _userSettings.StashTabIds = value;
                OnPropertyChanged(nameof(StashTabIds));
            }
        }
    }

    public string StashTabPrefix
    {
        get => _userSettings.StashTabPrefix;
        set
        {
            if (_userSettings.StashTabPrefix != value)
            {
                _userSettings.StashTabPrefix = value;
                OnPropertyChanged(nameof(StashTabPrefix));
            }
        }
    }

    public bool VendorSetsEarly
    {
        get => _userSettings.VendorSetsEarly;
        set
        {
            if (_userSettings.VendorSetsEarly != value)
            {
                _userSettings.VendorSetsEarly = value;
                OnPropertyChanged(nameof(VendorSetsEarly));
            }
        }
    }

    public bool SilenceSetsFullMessage
    {
        get => _userSettings.SilenceSetsFullMessage;
        set
        {
            if (_userSettings.SilenceSetsFullMessage != value)
            {
                _userSettings.SilenceSetsFullMessage = value;
                OnPropertyChanged(nameof(SilenceSetsFullMessage));
            }
        }
    }

    public bool SilenceNeedItemsMessage
    {
        get => _userSettings.SilenceNeedItemsMessage;
        set
        {
            if (_userSettings.SilenceNeedItemsMessage != value)
            {
                _userSettings.SilenceNeedItemsMessage = value;
                OnPropertyChanged(nameof(SilenceNeedItemsMessage));
            }
        }
    }

    public bool ChaosRecipeTrackingEnabled
    {
        get => _userSettings.ChaosRecipeTrackingEnabled;
        set
        {
            if (_userSettings.ChaosRecipeTrackingEnabled != value)
            {
                _userSettings.ChaosRecipeTrackingEnabled = value;
                OnPropertyChanged(nameof(ChaosRecipeTrackingEnabled));
            }
        }
    }

    #region Always Active Settings

    public bool LootFilterRingsAlwaysActive
    {
        get => _userSettings.LootFilterRingsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterRingsAlwaysActive != value)
            {
                _userSettings.LootFilterRingsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterRingsAlwaysActive));
            }
        }
    }

    public bool LootFilterAmuletsAlwaysActive
    {
        get => _userSettings.LootFilterAmuletsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterAmuletsAlwaysActive != value)
            {
                _userSettings.LootFilterAmuletsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterAmuletsAlwaysActive));
            }
        }
    }

    public bool LootFilterBeltsAlwaysActive
    {
        get => _userSettings.LootFilterBeltsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterBeltsAlwaysActive != value)
            {
                _userSettings.LootFilterBeltsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterBeltsAlwaysActive));
            }
        }
    }

    public bool LootFilterBodyArmourAlwaysActive
    {
        get => _userSettings.LootFilterBodyArmourAlwaysActive;
        set
        {
            if (_userSettings.LootFilterBodyArmourAlwaysActive != value)
            {
                _userSettings.LootFilterBodyArmourAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterBodyArmourAlwaysActive));
            }
        }
    }

    public bool LootFilterGlovesAlwaysActive
    {
        get => _userSettings.LootFilterGlovesAlwaysActive;
        set
        {
            if (_userSettings.LootFilterGlovesAlwaysActive != value)
            {
                _userSettings.LootFilterGlovesAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterGlovesAlwaysActive));
            }
        }
    }

    public bool LootFilterBootsAlwaysActive
    {
        get => _userSettings.LootFilterBootsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterBootsAlwaysActive != value)
            {
                _userSettings.LootFilterBootsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterBootsAlwaysActive));
            }
        }
    }

    public bool LootFilterHelmetsAlwaysActive
    {
        get => _userSettings.LootFilterHelmetsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterHelmetsAlwaysActive != value)
            {
                _userSettings.LootFilterHelmetsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterHelmetsAlwaysActive));
            }
        }
    }

    public bool LootFilterWeaponsAlwaysActive
    {
        get => _userSettings.LootFilterWeaponsAlwaysActive;
        set
        {
            if (_userSettings.LootFilterWeaponsAlwaysActive != value)
            {
                _userSettings.LootFilterWeaponsAlwaysActive = value;
                OnPropertyChanged(nameof(LootFilterWeaponsAlwaysActive));
            }
        }
    }

    #endregion

    #endregion

    private bool ShowAmountNeeded => SetTrackerOverlayItemCounterDisplayMode == Enums.SetTrackerOverlayItemCounterDisplayMode.ItemsMissing;
    public bool NeedsFetching => GlobalItemSetManagerState.NeedsFetching;
    public bool NeedsLowerLevel => GlobalItemSetManagerState.NeedsLowerLevel;
    public int FullSets => GlobalItemSetManagerState.CompletedSetCount;

    #region Item Amount and Visibility Properties

    #region Item Amount Properties

    public int RingsAmount => ShowAmountNeeded
        // case where we are showing missing items (calculate total needed and subtract from threshold, but don't show negatives)
        ? Math.Max((FullSetThreshold * 2) - GlobalItemSetManagerState.RingsAmount, 0)
        // case where we are showing total item sets (e.g. pair of rings as a single 'count')
        : GlobalItemSetManagerState.RingsAmount / 2;

    public int AmuletsAmount =>
        ShowAmountNeeded
            ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.AmuletsAmount, 0)
            : GlobalItemSetManagerState.AmuletsAmount;

    public int BeltsAmount =>
        ShowAmountNeeded
            ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.BeltsAmount, 0)
            : GlobalItemSetManagerState.BeltsAmount;

    public int ChestsAmount => ShowAmountNeeded
        ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.ChestsAmount, 0)
        : GlobalItemSetManagerState.ChestsAmount;

    public int GlovesAmount =>
        ShowAmountNeeded
            ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.GlovesAmount, 0)
            : GlobalItemSetManagerState.GlovesAmount;

    public int BootsAmount =>
    ShowAmountNeeded
        ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.BootsAmount, 0)
        : GlobalItemSetManagerState.BootsAmount;

    public int HelmetsAmount =>
        ShowAmountNeeded
            ? Math.Max(FullSetThreshold - GlobalItemSetManagerState.HelmetsAmount, 0)
            : GlobalItemSetManagerState.HelmetsAmount;

    public int WeaponsAmount => ShowAmountNeeded
        // case where we are showing missing items (calculate total needed and subtract from threshold, but don't show negatives)
        ? Math.Max((FullSetThreshold * 2) - (GlobalItemSetManagerState.WeaponsSmallAmount + (GlobalItemSetManagerState.WeaponsBigAmount * 2)), 0)
        // case where we are showing total weapon sets (e.g. pair of one handed weapons plus two handed weapons as a 'count' each)
        : (GlobalItemSetManagerState.WeaponsSmallAmount / 2) + GlobalItemSetManagerState.WeaponsBigAmount;

    #endregion

    #region Item Class Active (Visibility) Properties

    public bool RingsActive =>
        LootFilterRingsAlwaysActive ||
        (NeedsFetching || (FullSetThreshold * 2) - GlobalItemSetManagerState.RingsAmount > 0);

    public bool AmuletsActive =>
        LootFilterAmuletsAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.AmuletsAmount > 0);

    public bool BeltsActive =>
        LootFilterBeltsAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.BeltsAmount > 0);

    public bool ChestsActive =>
        LootFilterBodyArmourAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.ChestsAmount > 0);

    public bool GlovesActive =>
        LootFilterGlovesAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.GlovesAmount > 0);

    public bool HelmetsActive =>
        LootFilterHelmetsAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.HelmetsAmount > 0);

    public bool BootsActive =>
        LootFilterBootsAlwaysActive ||
        (NeedsFetching || FullSetThreshold - GlobalItemSetManagerState.BootsAmount > 0);

    public bool WeaponsActive =>
        LootFilterWeaponsAlwaysActive ||
        (NeedsFetching || (FullSetThreshold * 2) - (GlobalItemSetManagerState.WeaponsSmallAmount + (GlobalItemSetManagerState.WeaponsBigAmount * 2)) > 0);

    #endregion

    #endregion

    public bool FetchButtonEnabled
    {
        get => _fetchButtonEnabled;
        set => SetProperty(ref _fetchButtonEnabled, value);
    }

    public bool StashButtonTooltipEnabled
    {
        get => _stashButtonTooltipEnabled;
        set => SetProperty(ref _stashButtonTooltipEnabled, value);
    }

    public bool SetsTooltipEnabled
    {
        get => _setsTooltipEnabled;
        set => SetProperty(ref _setsTooltipEnabled, value);
    }

    public string WarningMessage
    {
        get => _warningMessage;
        set => SetProperty(ref _warningMessage, value);
    }

    #endregion

    #region Domain Methods

    public async Task<bool> FetchStashDataAsync()
    {
        WarningMessage = string.Empty;
        FetchButtonEnabled = false;

        try
        {
            // needed to update item set manager
            var setThreshold = FullSetThreshold;

            // have to do a bit of wizardry because we store the selected tab indices as a string in the user settings
            var filteredStashContents = new List<EnhancedItem>();

            // reset item amounts before fetching new data
            // invalidate some outdated state for our item manager
            GlobalItemSetManagerState.ResetCompletedSetCount();
            GlobalItemSetManagerState.ResetItemAmounts();

            // update the stash tab metadata based on your target stash
            var stashTabMetadataList = LegacyAuthMode
                ? GuildStashMode
                    ? await _apiService.GetAllGuildStashTabMetadataWithSessionIdAsync(LegacyAuthSessionId)
                    : await _apiService.GetAllPersonalStashTabMetadataWithSessionIdAsync(LegacyAuthSessionId)
                : GuildStashMode
                    ? await _apiService.GetAllGuildStashTabMetadataWithOAuthAsync()
                    : await _apiService.GetAllPersonalStashTabMetadataWithOAuthAsync();

            // 'Flatten' the stash tab structure (unwrap children tabs from folders)
            var flattenedStashTabs = GlobalItemSetManagerState.FlattenStashTabs(stashTabMetadataList);

            List<string> selectedTabIds = [];

            if (StashTabQueryMode == (int)Enums.StashTabQueryMode.TabsById)
            {
                if (StashTabIds.Count == 0)
                {
                    FetchButtonEnabled = true;

                    GlobalErrorHandler.Spawn(
                        "It looks like you haven't selected any stash tab ids. Please navigate to the 'General > General > Select Stash Tabs' setting and select some tabs, and try again.",
                        "Error: Set Tracker Overlay - Fetch Data"
                    );

                    return false;
                }

                selectedTabIds = [.. StashTabIds];
            }
            else if (StashTabQueryMode == (int)Enums.StashTabQueryMode.TabsByNamePrefix)
            {

                if (string.IsNullOrWhiteSpace(StashTabPrefix))
                {
                    FetchButtonEnabled = true;

                    GlobalErrorHandler.Spawn(
                        "It looks like you haven't entered a stash tab prefix. Please navigate to the 'General > General > Stash Tab Prefix' setting and enter a valid value, and try again.",
                        "Error: Set Tracker Overlay - Fetch Data"
                    );

                    return false;
                }

                selectedTabIds = flattenedStashTabs
                    .Where(st => st.Name.StartsWith(StashTabPrefix))
                    .Select(st => st.Id)
                    .ToList();
            }

            if (flattenedStashTabs is not null)
            {
                GlobalItemSetManagerState.UpdateStashMetadata(flattenedStashTabs);

                foreach (var id in selectedTabIds)
                {
                    UnifiedStashTabContents rawResults;

                    // Session ID endpoint uses tab index for lookup - so we 'extract' the index from the tab collection constructed using id's
                    if (LegacyAuthMode)
                    {
                        // For SessionId auth, we need to find the index corresponding to this id
                        var stashTab = flattenedStashTabs.FirstOrDefault(st => st.Id == id);

                        if (stashTab == null)
                        {
                            continue; // Skip this tab if we can't find its metadata
                        }

                        if (GuildStashMode)
                        {
                            rawResults = await _apiService.GetGuildStashTabContentsByIndexWithSessionIdAsync(
                                LegacyAuthSessionId,
                                stashTab.Id,
                                stashTab.Name,
                                stashTab.Index,
                                stashTab.Type
                            );
                        }
                        else
                        {
                            rawResults = await _apiService.GetPersonalStashTabContentsByIndexWithSessionIdAsync(
                                LegacyAuthSessionId,
                                stashTab.Id,
                                stashTab.Name,
                                stashTab.Index,
                                stashTab.Type
                            );
                        }

                    }
                    // OAuth endpoint uses tab ID for lookup
                    else
                    {
                        if (GuildStashMode)
                        {
                            rawResults = await _apiService.GetGuildStashTabContentsByStashIdWithOAuthAsync(id);
                        }
                        else
                        {
                            rawResults = await _apiService.GetPersonalStashTabContentsByStashIdWithOAuthAsync(id);
                        }
                    }

                    // then we convert the raw results into a list of EnhancedItem objects
                    var enhancedItems = rawResults.Items.Select(item => new EnhancedItem(item)).ToList();

                    // Manually setting id because we need to know which tab the item came from
                    foreach (var enhancedItem in enhancedItems)
                    {
                        enhancedItem.StashTabId = rawResults.Id;
                        enhancedItem.StashTabIndex = rawResults.Index;
                    }

                    // add the enhanced items to the filtered stash contents
                    filteredStashContents.AddRange(EnhancedItemUtilities.FilterItemsForRecipe(enhancedItems));

                    GlobalItemSetManagerState.UpdateStashContents(setThreshold, selectedTabIds, filteredStashContents);
                }

                // recalculate item amounts and generate item sets after fetching from api
                GlobalItemSetManagerState.CalculateItemAmounts();

                // generate item sets for the chosen recipe (chaos or regal)
                GlobalItemSetManagerState.GenerateItemSets(!ChaosRecipeTrackingEnabled);

                // update the UI accordingly
                UpdateDisplay();
                UpdateStashButtonAndWarningMessage();

                // enforce cooldown on fetch button to reduce chances of rate limiting
                TriggerSetTrackerFetchCooldown(FetchCooldownSeconds);
            }
            else
            {
                FetchButtonEnabled = true;
                return false;
            }
        }
        catch (RateLimitException e)
        {
            FetchButtonEnabled = false;
            WarningMessage = $"Rate Limit Exceeded! Waiting {e.SecondsToWait} seconds...";

            // Cooldown the refresh button until the rate limit is lifted
            TriggerSetTrackerFetchCooldown(e.SecondsToWait);

            WarningMessage = string.Empty;

            return true;
        }
        catch (NullReferenceException e)
        {
            FetchButtonEnabled = true;
            _authStateManager.Logout();
            GlobalErrorHandler.Spawn(
                e.ToString(),
                "Error: Set Tracker Overlay - Invalid Credentials",
                preamble: "It looks like your credentials have expired. Please log back in to continue."
            );
            return false;
        }

        return true;
    }

    public void UpdateStashButtonAndWarningMessage(bool playNotificationSound = true)
    {
        // case 1: user just opened the app, hasn't hit fetch yet
        if (NeedsFetching)
        {
            WarningMessage = string.Empty;
        }
        else if (!NeedsFetching)
        {
            // case 2: user fetched data and has enough sets to turn in based on their threshold
            if (FullSets >= FullSetThreshold)
            {
                // if the user has vendor sets early enabled, we don't want to show the warning message
                if (!VendorSetsEarly || FullSets >= FullSetThreshold)
                {
                    WarningMessage = !SilenceSetsFullMessage
                        ? SetsFullText
                        : string.Empty;
                }

                // stash button is enabled with no warning tooltip
                StashButtonTooltipEnabled = false;
                SetsTooltipEnabled = false;

                if (playNotificationSound)
                {
                    PlayItemSetStateChangedNotificationSound();
                }
            }

            // case 3: user fetched data and has at least 1 set, but not to their full threshold
            else if ((FullSets < FullSetThreshold || VendorSetsEarly) && FullSets >= 1)
            {
                WarningMessage = string.Empty;

                // stash button is disabled with warning tooltip to change threshold
                if (VendorSetsEarly)
                {
                    StashButtonTooltipEnabled = false;
                    SetsTooltipEnabled = false;
                }
                else
                {
                    StashButtonTooltipEnabled = true;
                    SetsTooltipEnabled = true;
                }
            }

            // case 3: user has fetched and needs items for chaos recipe (needs more lower level items)
            else if (NeedsLowerLevel && ChaosRecipeTrackingEnabled)
            {
                WarningMessage = !SilenceNeedItemsMessage
                    ? NeedsLowerLevelText(FullSets - FullSetThreshold)
                    : string.Empty;

                // stash button is disabled with conditional tooltip enabled
                // based on whether or not the user has at least 1 set
                StashButtonTooltipEnabled = FullSets >= 1;
                SetsTooltipEnabled = true;
            }

            // case 4: user has fetched and has no sets
            else if (FullSets == 0)
            {
                WarningMessage = string.Empty;

                // stash button is disabled with warning tooltip to change threshold
                StashButtonTooltipEnabled = true;
                SetsTooltipEnabled = true;
            }
        }
    }

    public async Task RunReloadFilter()
    {
        var itemClassAmounts = GlobalItemSetManagerState.RetrieveCurrentItemCountsForFilterManipulation();

        // hash set of missing item classes (e.g. "ring", "amulet", etc.)
        var missingItemClasses = new HashSet<string>();

        // first we check weapons since they're special and 2 item classes count for one
        var oneHandedWeaponCount = itemClassAmounts
            .Where(dict => dict.ContainsKey(Enums.ItemClass.OneHandWeapons))
            .Select(dict => dict[Enums.ItemClass.OneHandWeapons])
            .FirstOrDefault();

        var twoHandedWeaponCount = itemClassAmounts
            .Where(dict => dict.ContainsKey(Enums.ItemClass.TwoHandWeapons))
            .Select(dict => dict[Enums.ItemClass.TwoHandWeapons])
            .FirstOrDefault();

        if (oneHandedWeaponCount / 2 + twoHandedWeaponCount >= FullSetThreshold)
        {
            foreach (var dict in itemClassAmounts)
            {
                dict.Remove(Enums.ItemClass.OneHandWeapons);
                dict.Remove(Enums.ItemClass.TwoHandWeapons);
            }
        }

        foreach (var itemCountByClass in itemClassAmounts)
        {
            foreach (var itemClass in itemCountByClass)
            {
                if (itemClass.Value < FullSetThreshold)
                {
                    missingItemClasses.Add(itemClass.Key.ToString());
                }
            }
        }

        await _filterManipulationService.GenerateSectionsAndUpdateFilterAsync(missingItemClasses);
        _reloadFilterService.ReloadFilter();
    }

    #endregion

    #region Utility Methods

    private static string NeedsLowerLevelText(int diff) => $"Need {Math.Abs(diff)} items with iLvl 60-74!";

    private void UpdateDisplay()
    {
        OnPropertyChanged(nameof(RingsAmount));
        OnPropertyChanged(nameof(RingsActive));

        OnPropertyChanged(nameof(AmuletsAmount));
        OnPropertyChanged(nameof(AmuletsActive));

        OnPropertyChanged(nameof(BeltsAmount));
        OnPropertyChanged(nameof(BeltsActive));

        OnPropertyChanged(nameof(ChestsAmount));
        OnPropertyChanged(nameof(ChestsActive));

        OnPropertyChanged(nameof(WeaponsAmount));
        OnPropertyChanged(nameof(WeaponsActive));

        OnPropertyChanged(nameof(GlovesAmount));
        OnPropertyChanged(nameof(GlovesActive));

        OnPropertyChanged(nameof(HelmetsAmount));
        OnPropertyChanged(nameof(HelmetsActive));

        OnPropertyChanged(nameof(BootsAmount));
        OnPropertyChanged(nameof(BootsActive));

        OnPropertyChanged(nameof(NeedsFetching));
        OnPropertyChanged(nameof(NeedsLowerLevel));
        OnPropertyChanged(nameof(FullSets));
        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(FetchButtonEnabled));
        OnPropertyChanged(nameof(ShowAmountNeeded));
    }

    public void PlayItemSetStateChangedNotificationSound()
    {
        _notificationSoundService.PlayNotificationSound(Enums.NotificationSoundType.ItemSetStateChanged);
    }

    public void TriggerSetTrackerFetchCooldown(int secondsToWait)
    {
        // Ensure operation on the UI thread if called from another thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(secondsToWait)
            };

            // define the action to take when the timer ticks
            timer.Tick += (sender, args) =>
            {
                FetchButtonEnabled = true; // Re-enable the fetch button
                timer.Stop(); // Stop the timer to avoid it triggering again
                Log.Information("SetTrackerOverlayViewModel - Fetch cooldown timer has ended");
            };

            Log.Information("SetTrackerOverlayViewModel - Starting fetch cooldown timer for {SecondsToWait} seconds", secondsToWait);
            FetchButtonEnabled = false; // Disable the fetch button before starting the timer
            timer.Start(); // Start the cooldown timer
        });
    }

    #endregion
}
using System.Globalization;
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ui;

public enum PumasiTextKey
{
    SettingsTitle,
    SettingsLanguage,
    SettingsHostSubtitle,
    SettingsGuestSubtitle,
    SettingsFooter,
    GmcmUiSection,
    GmcmHelperSection,
    GmcmWorkCategoriesSection,
    GmcmGeminiSection,
    GmcmWikiAnswersSection,
    GmcmName,
    GmcmPersonality,
    GmcmBehaviorRules,
    GmcmMaxTasksPerDay,
    GmcmMorningTodos,
    GmcmCrops,
    GmcmMachines,
    GmcmAnimals,
    GmcmChests,
    GmcmPlanting,
    GmcmBaseUrl,
    GmcmModel,
    GmcmGeminiApiKey,
    GmcmGeminiApiKeyTooltip,
    GmcmMaxCallsPerDay,
    GmcmUseKoreanWikiAnswers,
    GmcmWikiBaseUrl,
    GmcmMaxWikiPages,
    GmcmWikiContextCharacterLimit,
    GmcmWikiQuestionCooldownSeconds,
    TodoIdle,
    HelpUsage,
    ApiKeyChatRejected,
    ApiKeyUsage,
    ApiKeySaved,
    GuestRequestSent,
    ScanQueuedTasks,
    TodoReorderHostOnly,
    TodoListEmpty,
    TodoReorderUsage,
    TodoReorderFailed,
    TodoOrderUnchanged,
    TodoMoved,
    WorkCategoryUsage,
    OnOffUsage,
    WorkCategorySaved,
    NoActiveTodos,
    UnknownReorderCommand,
    StatusMessage,
    WorkCategoryStatus,
    HostOnlyCommand,
    HostIdle,
    GuestView,
    MorningScanQueued,
    MorningScanFoundNoTodos,
    GeminiQueuedTasks,
    GeminiNotConfiguredForPlanning,
    ContextGeminiMissing,
    ChatAnswerFallback,
    ClarifyFallback,
    ContextReadError,
    WikiAnswersDisabled,
    WikiCooldown,
    WikiUnavailable,
    WikiNoResults,
    WikiGeminiMissingSummary,
    WikiSummaryFailed,
    WikiAnswerFailed,
    SourcePrefix
}

public static class PumasiText
{
    private static readonly Dictionary<PumasiTextKey, LocalizedText> Texts = new()
    {
        [PumasiTextKey.SettingsTitle] = new("품앗이 설정", "Pumasi Settings"),
        [PumasiTextKey.SettingsLanguage] = new("언어", "Language"),
        [PumasiTextKey.SettingsHostSubtitle] = new("품앗이 빠른 설정", "Quick helper settings"),
        [PumasiTextKey.SettingsGuestSubtitle] = new("게스트는 로컬 UI 설정만 변경할 수 있어요", "Guests can only change local UI settings"),
        [PumasiTextKey.SettingsFooter] = new("이름/규칙/API 키: GMCM 또는 SMAPI 콘솔 pms_key", "Name/rules/API key: GMCM or SMAPI console pms_key"),
        [PumasiTextKey.GmcmUiSection] = new("UI", "UI"),
        [PumasiTextKey.GmcmHelperSection] = new("도우미", "Helper"),
        [PumasiTextKey.GmcmWorkCategoriesSection] = new("작업 카테고리", "Work Categories"),
        [PumasiTextKey.GmcmGeminiSection] = new("Gemini", "Gemini"),
        [PumasiTextKey.GmcmWikiAnswersSection] = new("위키 답변", "Wiki Answers"),
        [PumasiTextKey.GmcmName] = new("이름", "Name"),
        [PumasiTextKey.GmcmPersonality] = new("성격", "Personality"),
        [PumasiTextKey.GmcmBehaviorRules] = new("행동 규칙", "Behavior rules"),
        [PumasiTextKey.GmcmMaxTasksPerDay] = new("하루 최대 작업 수", "Max tasks per day"),
        [PumasiTextKey.GmcmMorningTodos] = new("아침 투두 수", "Morning todos"),
        [PumasiTextKey.GmcmCrops] = new("작물", "Crops"),
        [PumasiTextKey.GmcmMachines] = new("기계", "Machines"),
        [PumasiTextKey.GmcmAnimals] = new("동물", "Animals"),
        [PumasiTextKey.GmcmChests] = new("상자", "Chests"),
        [PumasiTextKey.GmcmPlanting] = new("씨앗 심기", "Planting"),
        [PumasiTextKey.GmcmBaseUrl] = new("Base URL", "Base URL"),
        [PumasiTextKey.GmcmModel] = new("모델", "Model"),
        [PumasiTextKey.GmcmGeminiApiKey] = new("Gemini API 키", "Gemini API key"),
        [PumasiTextKey.GmcmGeminiApiKeyTooltip] = new("이 호스트의 로컬 config에만 저장됩니다. 새 키를 입력하면 기존 키를 교체합니다.", "Stored only in this host's local config. Enter a new key to replace the existing one."),
        [PumasiTextKey.GmcmMaxCallsPerDay] = new("하루 최대 호출 수", "Max calls per day"),
        [PumasiTextKey.GmcmUseKoreanWikiAnswers] = new("한국어 위키 답변 사용", "Use Korean Wiki answers"),
        [PumasiTextKey.GmcmWikiBaseUrl] = new("위키 Base URL", "Wiki base URL"),
        [PumasiTextKey.GmcmMaxWikiPages] = new("최대 위키 페이지 수", "Max wiki pages"),
        [PumasiTextKey.GmcmWikiContextCharacterLimit] = new("위키 문맥 글자 제한", "Wiki context character limit"),
        [PumasiTextKey.GmcmWikiQuestionCooldownSeconds] = new("위키 질문 쿨다운 초", "Wiki question cooldown seconds"),
        [PumasiTextKey.TodoIdle] = new("투두: 대기 중", "Todo: idle"),
        [PumasiTextKey.HelpUsage] = new("사용법: /pms status, /pms scan, /pms todo, /pms todo move 3 1, /pms animals on|off, /pms ask <질문/작업>, /pms <질문/작업>", "Usage: /pms status, /pms scan, /pms todo, /pms todo move 3 1, /pms animals on|off, /pms ask <question/work>, /pms <question/work>"),
        [PumasiTextKey.ApiKeyChatRejected] = new("API KEY는 인게임 채팅에 입력하지 말고 SMAPI 콘솔의 pms_key <key> 또는 config.json에서 설정해줘요.", "Do not enter API keys in in-game chat. Use the SMAPI console pms_key <key> command or config.json."),
        [PumasiTextKey.ApiKeyUsage] = new("사용법: pms_key <gemini-api-key>", "Usage: pms_key <gemini-api-key>"),
        [PumasiTextKey.ApiKeySaved] = new("Gemini API 키를 로컬에 저장했어요. 게스트에게는 동기화되지 않습니다.", "Gemini API key saved locally. It will not be synced to guests."),
        [PumasiTextKey.GuestRequestSent] = new("호스트에게 Pumasi 요청을 보냈어요.", "Sent the Pumasi request to the host."),
        [PumasiTextKey.ScanQueuedTasks] = new("스캔한 작업 {0}개를 투두에 추가했어요.", "Queued {0} scanned task(s)."),
        [PumasiTextKey.TodoReorderHostOnly] = new("투두 순서 변경은 호스트 전용입니다. 게스트는 동기화된 투두 목록을 볼 수 있어요.", "Todo reorder is host-only. Guests can view the synced todo list."),
        [PumasiTextKey.TodoListEmpty] = new("투두 목록이 비어 있어요.", "Todo list is empty."),
        [PumasiTextKey.TodoReorderUsage] = new("투두 순서 변경 사용법: /pms todo move <from> <to>, /pms todo up <index>, /pms todo down <index>, /pms todo top <index>, /pms todo bottom <index>. {0}", "Todo reorder usage: /pms todo move <from> <to>, /pms todo up <index>, /pms todo down <index>, /pms todo top <index>, /pms todo bottom <index>. {0}"),
        [PumasiTextKey.TodoReorderFailed] = new("투두 순서 변경 실패: {0}", "Todo reorder failed: {0}"),
        [PumasiTextKey.TodoOrderUnchanged] = new("투두 순서가 그대로예요.", "Todo order unchanged."),
        [PumasiTextKey.TodoMoved] = new("투두 #{0}을 #{1} 위치로 옮겼어요.", "Moved todo #{0} to #{1}."),
        [PumasiTextKey.WorkCategoryUsage] = new("작업 카테고리 사용법: /pms animals on|off 또는 /pms work animals on|off. 카테고리: crops, machines, animals, chests, planting.", "Work category usage: /pms animals on|off or /pms work animals on|off. Categories: crops, machines, animals, chests, planting."),
        [PumasiTextKey.OnOffUsage] = new("on/off, enable/disable, true/false, 또는 켜/꺼를 사용해 주세요.", "Use on/off, enable/disable, true/false, or 켜/꺼."),
        [PumasiTextKey.WorkCategorySaved] = new("{0} 작업 카테고리를 {1} 상태로 저장했어요.", "Saved {0} work category as {1}."),
        [PumasiTextKey.NoActiveTodos] = new("활성 투두가 없어요.", "There are no active todos."),
        [PumasiTextKey.UnknownReorderCommand] = new("알 수 없는 순서 변경 명령입니다.", "Unknown reorder command."),
        [PumasiTextKey.StatusMessage] = new("pumasi (품앗이): 호스트={0}, 모드={1}, Gemini설정={2}, 투두={3}, 대화={4}", "pumasi (품앗이): host={0}, mode={1}, geminiConfigured={2}, todos={3}, contextTurns={4}"),
        [PumasiTextKey.WorkCategoryStatus] = new("작업: 작물={0}, 기계={1}, 동물={2}, 상자={3}, 씨앗심기={4}", "work: crops={0}, machines={1}, animals={2}, chests={3}, planting={4}"),
        [PumasiTextKey.HostOnlyCommand] = new("이 명령은 호스트 전용입니다. 게스트는 /pms ask로 요청을 보낼 수 있어요.", "This command is host-only. Guests send commands through /pms ask."),
        [PumasiTextKey.HostIdle] = new("호스트 대기 중", "Host idle"),
        [PumasiTextKey.GuestView] = new("게스트 보기", "Guest view"),
        [PumasiTextKey.MorningScanQueued] = new("아침 스캔으로 투두 {0}개를 추가했어요", "Morning scan queued {0} todo(s)"),
        [PumasiTextKey.MorningScanFoundNoTodos] = new("아침 스캔에서 새 투두를 찾지 못했어요", "Morning scan found no new todos"),
        [PumasiTextKey.GeminiQueuedTasks] = new("Gemini가 작업 {0}개를 투두에 추가했어요", "Gemini queued {0} task(s)"),
        [PumasiTextKey.GeminiNotConfiguredForPlanning] = new("Gemini가 아직 설정되어 있지 않아요. pms_key <key>를 실행하고 config.json의 모델/Base URL 설정을 확인해 주세요.", "Gemini is not configured. Run pms_key <key> and check config.json for model/base URL settings."),
        [PumasiTextKey.ContextGeminiMissing] = new("이 말은 맥락을 봐야 이해할 수 있는데 Gemini가 아직 설정되어 있지 않아요. SMAPI 콘솔에서 pms_key <key>를 먼저 설정해줘요.", "I need conversation context to understand that, but Gemini is not configured yet. Set it first with pms_key <key> in the SMAPI console."),
        [PumasiTextKey.ChatAnswerFallback] = new("응, 듣고 있어요.", "I'm listening."),
        [PumasiTextKey.ClarifyFallback] = new("조금만 더 알려줘요. 맥락을 보고도 확신하기 어려워요.", "Please tell me a little more. I still cannot infer it safely from context."),
        [PumasiTextKey.ContextReadError] = new("대화 맥락을 읽는 중 문제가 생겼어요. 잠깐 뒤에 다시 말해줘요.", "I had trouble reading the conversation context. Please try again in a moment."),
        [PumasiTextKey.WikiAnswersDisabled] = new("위키 기반 답변 기능이 꺼져 있어요.", "Wiki-based answers are turned off."),
        [PumasiTextKey.WikiCooldown] = new("위키 질문은 잠깐 쉬었다가 다시 물어봐 주세요.", "Please wait a moment before asking another wiki question."),
        [PumasiTextKey.WikiUnavailable] = new("지금은 위키에 접속할 수 없어서 확인하지 못했어요.", "I cannot reach the wiki right now."),
        [PumasiTextKey.WikiNoResults] = new("한국어 위키에서 관련 내용을 찾지 못했어요.", "I could not find a related page on the Korean wiki."),
        [PumasiTextKey.WikiGeminiMissingSummary] = new("위키 페이지는 찾았지만 Gemini가 설정되어 있지 않아 요약할 수 없어요.", "I found wiki pages, but Gemini is not configured so I cannot summarize them."),
        [PumasiTextKey.WikiSummaryFailed] = new("위키 자료는 찾았지만 답변 요약을 만들지 못했어요. 출처를 확인해 주세요.", "I found wiki material, but could not summarize an answer. Please check the sources."),
        [PumasiTextKey.WikiAnswerFailed] = new("지금은 위키 기반 답변을 만들 수 없어요.", "I cannot create a wiki-based answer right now."),
        [PumasiTextKey.SourcePrefix] = new("출처", "Sources")
    };

    private static readonly Dictionary<TaskType, LocalizedText> TaskTypeTexts = new()
    {
        [TaskType.HarvestCrop] = new("작물 수확", "Harvest crop"),
        [TaskType.WaterCrop] = new("작물 물주기", "Water crop"),
        [TaskType.TillSprinklerSoil] = new("스프링클러 주변 땅 파기", "Till sprinkler soil"),
        [TaskType.CollectMachine] = new("기계 수거", "Collect machine"),
        [TaskType.RefillMachine] = new("기계 리필", "Refill machine"),
        [TaskType.RefillHay] = new("건초 리필", "Refill hay"),
        [TaskType.PetAnimal] = new("동물 쓰다듬기", "Pet animal"),
        [TaskType.CollectAnimalProduct] = new("동물 생산품 수거", "Collect animal product"),
        [TaskType.SortChest] = new("상자 정리", "Sort chest"),
        [TaskType.PlantSeed] = new("씨앗 심기", "Plant seed")
    };

    private static readonly Dictionary<HelperTaskStatus, LocalizedText> TaskStatusTexts = new()
    {
        [HelperTaskStatus.Proposed] = new("제안", "Proposed"),
        [HelperTaskStatus.Queued] = new("대기", "Queued"),
        [HelperTaskStatus.Claimed] = new("확보", "Claimed"),
        [HelperTaskStatus.InProgress] = new("진행 중", "In progress"),
        [HelperTaskStatus.Completed] = new("완료", "Completed"),
        [HelperTaskStatus.Skipped] = new("건너뜀", "Skipped"),
        [HelperTaskStatus.Failed] = new("실패", "Failed"),
        [HelperTaskStatus.Cancelled] = new("취소", "Cancelled")
    };

    private static readonly Dictionary<string, LocalizedText> ExecutionReasonTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["location-not-loaded"] = new("위치를 불러오지 못했어요", "Location was not loaded"),
        ["task-type-not-implemented-in-mvp"] = new("아직 구현되지 않은 작업이에요", "Task type is not implemented yet"),
        ["crop-tile-not-found"] = new("작물 타일을 찾지 못했어요", "Crop tile was not found"),
        ["crop-already-gone"] = new("작물이 이미 없어졌어요", "Crop is already gone"),
        ["crop-already-watered"] = new("작물에 이미 물이 있어요", "Crop is already watered"),
        ["watered-crop"] = new("작물에 물을 줬어요", "Watered crop"),
        ["crop-harvest-method-failed"] = new("작물 수확에 실패했어요", "Crop harvest failed"),
        ["harvested-crop"] = new("작물을 수확했어요", "Harvested crop"),
        ["machine-not-found"] = new("기계를 찾지 못했어요", "Machine was not found"),
        ["machine-not-ready"] = new("기계가 아직 준비되지 않았어요", "Machine is not ready"),
        ["collected-machine"] = new("기계 생산품을 수거했어요", "Collected machine output"),
        ["machine-check-action-returned-false"] = new("기계 수거에 실패했어요", "Machine collection failed"),
        ["tile-has-object"] = new("타일에 물건이 있어요", "Tile has an object"),
        ["soil-already-tilled"] = new("이미 파인 땅이에요", "Soil is already tilled"),
        ["tile-has-terrain-feature"] = new("타일에 지형 요소가 있어요", "Tile has a terrain feature"),
        ["tile-not-diggable"] = new("팔 수 없는 타일이에요", "Tile is not diggable"),
        ["tilled-sprinkler-soil"] = new("스프링클러 주변 땅을 팠어요", "Tilled sprinkler soil"),
        ["not-an-animal-house"] = new("동물 건물이 아니에요", "Not an animal building"),
        ["hay-refill-method-not-found"] = new("건초 리필 방법을 찾지 못했어요", "Hay refill method was not found"),
        ["refilled-hay"] = new("건초를 리필했어요", "Refilled hay")
    };

    public static string Get(UiLanguage language, PumasiTextKey key)
    {
        return Texts.TryGetValue(key, out var value)
            ? value.Get(language)
            : key.ToString();
    }

    public static string Format(UiLanguage language, PumasiTextKey key, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, Get(language, key), args);
    }

    public static string GetLanguageName(UiLanguage displayLanguage, UiLanguage value)
    {
        return displayLanguage == UiLanguage.English
            ? value == UiLanguage.English ? "English" : "Korean"
            : value == UiLanguage.English ? "영어" : "한국어";
    }

    public static string GetTaskType(UiLanguage language, TaskType type)
    {
        return TaskTypeTexts.TryGetValue(type, out var value) ? value.Get(language) : type.ToString();
    }

    public static string GetTaskStatus(UiLanguage language, HelperTaskStatus status)
    {
        return TaskStatusTexts.TryGetValue(status, out var value) ? value.Get(language) : status.ToString();
    }

    public static string GetExecutionReason(UiLanguage language, string reason)
    {
        if (ExecutionReasonTexts.TryGetValue(reason, out var value))
            return value.Get(language);

        return reason;
    }

    public static string FormatOnOff(UiLanguage language, bool enabled)
    {
        return language == UiLanguage.English
            ? enabled ? "on" : "off"
            : enabled ? "켜짐" : "꺼짐";
    }

    private sealed record LocalizedText(string Korean, string English)
    {
        public string Get(UiLanguage language) => language == UiLanguage.English ? English : Korean;
    }
}

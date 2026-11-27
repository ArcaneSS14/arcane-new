using Content.Shared.TTS;
using Content.Arcane.Shared.TTS;
using Content.Arcane.Common.CCVar;
using Content.Server.Players.RateLimiting;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Arcane.Server.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private PlayerRateLimitManager _previewRateLimit = default!;
    private const string PreviewRateLimitKey = "TTSPreview";

    private void InitializePreviewRateLimit()
    {
        _previewRateLimit.Register(PreviewRateLimitKey,
            new RateLimitRegistration(ArcaneCVars.TTSRateLimitPeriod, ArcaneCVars.TTSRateLimitCount, null));
    }

    private readonly List<string> _sampleText = new() // TODO: Локализация?
    {
        "Съешь же ещё этих мягких французских булок, да выпей чаю.",
        "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
        "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
        "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
        "Учёные, тут странная аномалия в баре! Она уже съела мима!",
        "Я надеюсь что инженеры внимательно следят за сингулярностью...",
        "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
        "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
        "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
        "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
        "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
        "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
    };

    /// <summary>Rate-limits preview requests per session before consulting the cache or provider.</summary>
    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled || !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (_previewRateLimit.CountAction(args.SenderSession, PreviewRateLimitKey) != RateLimitStatus.Allowed)
            return;

        var txt = _robustRandom.Pick(_sampleText);
        var cacheId = GetCacheId(protoVoice, $"{VoiceRequestType.Preview.ToString()}-{_sampleText.IndexOf(txt)}");

        var cached = await GetFromCache(cacheId);
        if (cached != null)
        {
            RaiseNetworkEvent(new PlayTTSEvent(cached, isPreview: true), Filter.SinglePlayer(args.SenderSession));
            return;
        }

        var soundData = await GenerateTTS(txt, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, isPreview: true), Filter.SinglePlayer(args.SenderSession), false);

        await SaveVoiceCache(cacheId, soundData);
    }
}

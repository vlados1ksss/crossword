mergeInto(LibraryManager.library, {
  // Язык интерфейса Яндекс Игр (ysdk.environment.i18n.lang), например "ru", "en", "tr".
  // SDK инициализирует PluginYourGames 2: WebGL-шаблон YandexGames хранит экземпляр в глобальной переменной ysdk.
  // Возвращает пустую строку, если SDK ещё не готов или язык неизвестен.
  CrosswordGetSdkLanguage: function () {
    var lang = '';
    try {
      if (typeof ysdk !== 'undefined' && ysdk !== null && ysdk.environment && ysdk.environment.i18n)
        lang = ysdk.environment.i18n.lang || '';
    } catch (e) {
      console.warn('[Loc] Failed to read SDK language', e);
    }
    var size = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(size);
    stringToUTF8(lang, buffer, size);
    return buffer;
  }
});

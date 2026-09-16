mergeInto(LibraryManager.library, {
  // Сохраняет содержимое persistentDataPath (IDBFS) в IndexedDB браузера.
  CrosswordSyncFiles: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) {
      FS.syncfs(false, function (err) {
        if (err) console.warn('Crossword save sync error: ' + err);
      });
    }
  }
});

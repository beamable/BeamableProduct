mergeInto(LibraryManager.library, {
    SyncToIndexedDB: function () {
        FS.syncfs(false, function(err) {
            if (err) {
                console.error("Error syncing to IndexedDB:", err);
            } else {
                console.log("FS synced to IndexedDB successfully.");
            }
        });
    }
});
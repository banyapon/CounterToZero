mergeInto(LibraryManager.library, {
  CTZ_IsMobileBrowser: function () {
    return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent) ||
      (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1) ? 1 : 0;
  }
});

mergeInto(LibraryManager.library, {
  WebGL_IsMobileBrowser: function () {
    try {
      var ua = (typeof navigator !== 'undefined' && (navigator.userAgent || navigator.vendor || '')) || '';
      if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile|Mobile Safari/i.test(ua)) {
        return 1;
      }

      // iPadOS 13+ 常伪装成 Macintosh，但带多点触控
      if (typeof navigator !== 'undefined' && navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1) {
        return 1;
      }

      // 微信 / 抖音 / QQ 等小游戏或内置 WebView 容器
      if (/MiniProgram|MicroMessenger|ToutiaoMicroApp|BytedanceWebview|Aweme|TTWebView/i.test(ua)) {
        return 1;
      }

      return 0;
    } catch (e) {
      return 0;
    }
  }
});

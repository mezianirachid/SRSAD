var documentViewer;
$(document).ready(function () {
    var preferences = new gnostice.Preferences();
    // Specifies pixel density of the content displayed in the viewer.
    preferences.renderingDpi = 150;
    preferences.printingDpi = 200;
    //Specifies whether toolbar of the viewer should be displayed.
    preferences.toolbarVisible = true;
    //Specifies whether form filling should be enabled.
    preferences.interactiveElements.formFields.enableFormFilling = true;
    // Specifies highlight color for the interactive form fields.
    preferences.interactiveElements.formFields.highlightColor = "rgba(204, 215, 255, 0.5)";
    //Specifies whether annotations interactivity should be enabled.
    preferences.interactiveElements.annotations.enableAnnotations = true;
    //Specifies the user identity which is to be used while editing annotations.
    preferences.userIdentity.name = "Anonymous";
    //Specifies OCR settings for text search capability on scanned documents.
    preferences.digitizerSettings.digitizationEnabled = true;
    preferences.digitizerSettings.textLanguage = "eng";
    //Specifies whether color inversion controls needs to be displayed.
    preferences.visibleColorInversionControls.allPages = true;
    //Specifies whether file operation controls needs to be displayed.
    preferences.visibleFileOperationControls.download = true;
    preferences.visibleFileOperationControls.downloadAs = true;
    preferences.visibleFileOperationControls.open = true;
    preferences.visibleFileOperationControls.print = true;
    preferences.visibleFileOperationControls.save = true;
    //Specifies whether full screen control needs to be displayed.
    preferences.visibleFullScreen = true;
    //Specifies whether page navigation controls needs to be displayed.
    preferences.visibleNavigationControls.firstPage = true;
    preferences.visibleNavigationControls.gotoPage = true;
    preferences.visibleNavigationControls.lastPage = true;
    preferences.visibleNavigationControls.nextPage = true;
    preferences.visibleNavigationControls.pageIndicator = true;
    preferences.visibleNavigationControls.prevPage = true;
    //Specifies whether page magnification controls needs to be displayed.
    preferences.visibleZoomControls.fixedSteps = true;
    preferences.visibleZoomControls.zoomIn = true;
    preferences.visibleZoomControls.zoomOut = true;
    //Specifies whether page rotation operation controls needs to be displayed.
    preferences.visibleRotationControls.clockwise = true;
    preferences.visibleRotationControls.counterClockwise = true;
    //Specifies search operation controls settings.
    preferences.search.enableQuickSearch = true;
    preferences.search.visibleQuickSearchControls = true;
    preferences.search.highlightColor = "yellow";
    // Specifies initial view settings with which document when loaded needs to be displayed.
    preferences.viewSettings.zoomValue = gnostice.ZoomMode.actualSize;
    preferences.viewSettings.rotationAngle = gnostice.RotationAngle.zero;
    preferences.viewSettings.colorInversionApplied = false;
    preferences.viewSettings.navigationPaneOpened = true;
    preferences.navigationPane.enableBookmarks = true;
    preferences.navigationPane.enableThumbnails = true;
    preferences.navigationPane.visible = true;
    preferences.navigationPane.position = gnostice.NavigationPanePosition.fixed;
    preferences.navigationPane.width = 210;
    // Specifies hyperlink access settings.
    preferences.securitySettings.hyperlinkNavigation.access = gnostice.HyperlinkAccess.alwaysAsk;
	preferences.localization.language = "en_US";
    preferences.localization.resourcePath = "Scripts/localization/";
    documentViewer = new gnostice.DocumentViewer('doc-viewer-id', preferences);
});

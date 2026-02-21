import 'package:flutter/foundation.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'api_service.dart';
import '../models/support_status.dart';

class AppMetadataService extends ChangeNotifier {
  ApiService _apiService;
  PackageInfo? _packageInfo;
  SupportStatus? _supportStatus;
  bool _isLoadingSupport = false;

  AppMetadataService(this._apiService);

  String get version => _packageInfo?.version ?? 'Unknown';
  String get buildNumber => _packageInfo?.buildNumber ?? 'Unknown';
  String get appName => _packageInfo?.appName ?? 'Valora';
  String get packageName => _packageInfo?.packageName ?? 'Unknown';

  SupportStatus? get supportStatus => _supportStatus;
  bool get isLoadingSupport => _isLoadingSupport;

  Future<void> init() async {
    _packageInfo = await PackageInfo.fromPlatform();
    // Fetch support status on init, but don't block
    fetchSupportStatus();
    notifyListeners();
  }

  Future<void> fetchSupportStatus() async {
    if (_isLoadingSupport) return;
    _isLoadingSupport = true;
    notifyListeners();

    try {
      _supportStatus = await _apiService.getSupportStatus();
    } catch (e) {
      _supportStatus = SupportStatus.fallback();
    } finally {
      _isLoadingSupport = false;
      notifyListeners();
    }
  }

  void update(ApiService apiService) {
    _apiService = apiService;
    // Don't necessarily notify here unless we want to trigger a re-fetch
  }
}

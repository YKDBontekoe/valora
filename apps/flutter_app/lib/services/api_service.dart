import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';

import '../core/config/app_config.dart';
import '../models/context_report.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../models/listing_response.dart';
import '../models/map_amenity.dart';
import '../models/map_city_insight.dart';
import '../models/map_overlay.dart';
import '../models/notification.dart';
import 'http/clients/ai_api_client.dart';
import 'http/clients/context_report_api_client.dart';
import 'http/clients/listings_api_client.dart';
import 'http/clients/map_api_client.dart';
import 'http/clients/notifications_api_client.dart';
import 'http/core/api_runner.dart';
import 'http/core/http_transport.dart';

class ApiService {
  ApiService({
    String? authToken,
    Future<String?> Function()? refreshTokenCallback,
    http.Client? client,
    ApiRunner? runner,
    RetryOptions? retryOptions,
  }) : _authToken = authToken {
    final HttpTransport transport = HttpTransport(
      client: client ?? http.Client(),
      authTokenReader: (() => _authToken),
      refreshToken: refreshTokenCallback == null
          ? null
          : () async {
              final String? refreshed = await refreshTokenCallback();
              _authToken = refreshed;
              return refreshed;
            },
      retryOptions: retryOptions,
    );

    final ApiRunner parserRunner = runner ?? defaultApiRunner;
    _listingsApiClient = ListingsApiClient(transport: transport, runner: parserRunner);
    _contextReportApiClient = ContextReportApiClient(transport: transport, runner: parserRunner);
    _mapApiClient = MapApiClient(transport: transport);
    _notificationsApiClient = NotificationsApiClient(transport: transport);
    _aiApiClient = AiApiClient(transport: transport);
  }



  ApiService.fromClients({
    required ListingsApiClient listingsApiClient,
    required ContextReportApiClient contextReportApiClient,
    required MapApiClient mapApiClient,
    required NotificationsApiClient notificationsApiClient,
    required AiApiClient aiApiClient,
  })  : _authToken = null,
        _listingsApiClient = listingsApiClient,
        _contextReportApiClient = contextReportApiClient,
        _mapApiClient = mapApiClient,
        _notificationsApiClient = notificationsApiClient,
        _aiApiClient = aiApiClient;
  static String get baseUrl => AppConfig.apiUrl;
  String? _authToken;

  late final ListingsApiClient _listingsApiClient;
  late final ContextReportApiClient _contextReportApiClient;
  late final MapApiClient _mapApiClient;
  late final NotificationsApiClient _notificationsApiClient;
  late final AiApiClient _aiApiClient;

  Future<ListingResponse> getListings(
    ListingFilter filter, {
    int page = 1,
    int pageSize = 20,
  }) {
    return _listingsApiClient.getListings(filter, page: page, pageSize: pageSize);
  }

  Future<Listing> getListing(String id) {
    return _listingsApiClient.getListing(id);
  }

  Future<Listing?> getListingFromPdok(String id) {
    return _listingsApiClient.getListingFromPdok(id);
  }

  Future<ContextReport> getContextReport(
    String input, {
    int radiusMeters = 1000,
  }) {
    return _contextReportApiClient.getContextReport(input, radiusMeters: radiusMeters);
  }

  Future<String> getAiAnalysis(ContextReport report) {
    return _aiApiClient.getAiAnalysis(report);
  }

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) {
    return _notificationsApiClient.getNotifications(
      unreadOnly: unreadOnly,
      limit: limit,
      offset: offset,
    );
  }

  Future<int> getUnreadNotificationCount() {
    return _notificationsApiClient.getUnreadNotificationCount();
  }

  Future<void> markNotificationAsRead(String id) {
    return _notificationsApiClient.markNotificationAsRead(id);
  }

  Future<void> markAllNotificationsAsRead() {
    return _notificationsApiClient.markAllNotificationsAsRead();
  }

  Future<void> deleteNotification(String id) {
    return _notificationsApiClient.deleteNotification(id);
  }

  Future<bool> healthCheck() {
    return _listingsApiClient.healthCheck();
  }

  Future<List<MapCityInsight>> getCityInsights() {
    return _mapApiClient.getCityInsights();
  }

  Future<List<MapAmenity>> getMapAmenities({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    List<String>? types,
  }) {
    return _mapApiClient.getMapAmenities(
      minLat: minLat,
      minLon: minLon,
      maxLat: maxLat,
      maxLon: maxLon,
      types: types,
    );
  }

  Future<List<MapOverlay>> getMapOverlays({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required String metric,
  }) {
    return _mapApiClient.getMapOverlays(
      minLat: minLat,
      minLon: minLon,
      maxLat: maxLat,
      maxLon: maxLon,
      metric: metric,
    );
  }
}

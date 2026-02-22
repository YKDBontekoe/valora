import 'dart:async';
import 'dart:io';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:valora_app/models/map_overlay.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';
import 'package:valora_app/models/map_query_result.dart';

// Create a mock provider that extends ChangeNotifier to support listeners
class MockInsightsProvider extends ChangeNotifier implements InsightsProvider {
  @override
  bool showOverlays = false;
  @override
  bool showAmenities = false;
  @override
  List<MapOverlay> overlays = [];
  @override
  List<MapOverlayTile> overlayTiles = [];
  @override
  List<MapAmenity> amenities = [];
  @override
  List<MapAmenityCluster> amenityClusters = [];
  @override
  List<MapCityInsight> cities = [];
  @override
  MapOverlayMetric selectedOverlayMetric = MapOverlayMetric.pricePerSquareMeter;
  @override
  bool isLoading = false;
  @override
  bool isQuerying = false;
  @override
  String? error;
  @override
  String? mapError;
  @override
  MapQueryResult? lastQueryResult;

  @override
  double? getScore(MapCityInsight city) => city.compositeScore;

  // Implement other required members
  @override
  InsightMetric get selectedMetric => InsightMetric.composite;

  @override
  void update(dynamic apiService) {}

  @override
  Future<void> loadInsights() async {}

  @override
  Future<void> fetchMapData({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    double zoom = 7.5,
  }) async {}

  @override
  Future<void> performMapQuery(
    String prompt, {
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
  }) async {}

  @override
  void clearQueryResult() {}

  @override
  void toggleAmenities() {}

  @override
  void toggleOverlays() {}

  @override
  void setOverlayMetric(MapOverlayMetric metric) {}

  @override
  void setMetric(InsightMetric metric) {}
}

void main() {
  setUpAll(() {
    // Override HTTP client to block network calls from flutter_map TileLayer
    HttpOverrides.global = _MockHttpOverrides();
  });

  testWidgets('InsightsMap renders tiles when overlayTiles are present', (WidgetTester tester) async {
    final provider = MockInsightsProvider();
    provider.showOverlays = true;
    provider.overlayTiles = [
      MapOverlayTile(
        latitude: 52.0,
        longitude: 5.0,
        size: 0.1,
        value: 100,
        displayValue: '100',
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>.value(
          value: provider,
          child: InsightsMap(
            mapController: MapController(),
            onMapChanged: () {},
          ),
        ),
      ),
    );

    await tester.pump(); // Allow map to build

    // Verify that PolygonLayer is present
    expect(find.byType(PolygonLayer), findsOneWidget);
  });

  testWidgets('InsightsMap renders clusters when amenityClusters are present', (WidgetTester tester) async {
    final provider = MockInsightsProvider();
    provider.showAmenities = true;
    provider.amenityClusters = [
      MapAmenityCluster(
        latitude: 52.0,
        longitude: 5.0,
        count: 10,
        typeCounts: {'school': 10},
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>.value(
          value: provider,
          child: InsightsMap(
            mapController: MapController(),
            onMapChanged: () {},
          ),
        ),
      ),
    );

    await tester.pump();

    // Verify that MarkerLayer is present (for clusters)
    // There's always one MarkerLayer for cities, so we expect 2 if clusters are shown
    expect(find.byType(MarkerLayer), findsAtLeastNWidgets(2));
  });
}

class _MockHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return _MockHttpClient();
  }
}

class _MockHttpClient implements HttpClient {
  @override
  bool autoUncompress = true;
  @override
  Duration? connectionTimeout;
  @override
  Duration idleTimeout = const Duration(seconds: 15);
  @override
  int? maxConnectionsPerHost;
  @override
  String? userAgent;
  @override
  dynamic connectionFactory;

  @override
  void addCredentials(Uri url, String realm, HttpClientCredentials credentials) {}

  @override
  void addProxyCredentials(String host, int port, String realm, HttpClientCredentials credentials) {}

  @override
  set authenticate(Future<bool> Function(Uri url, String scheme, String? realm)? f) {}

  @override
  set authenticateProxy(Future<bool> Function(String host, int port, String scheme, String? realm)? f) {}

  @override
  set badCertificateCallback(bool Function(X509Certificate cert, String host, int port)? callback) {}

  @override
  void close({bool force = false}) {}

  @override
  Future<HttpClientRequest> delete(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> deleteUrl(Uri url) => throw UnimplementedError();

  @override
  set findProxy(String Function(Uri url)? f) {}

  @override
  Future<HttpClientRequest> get(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> getUrl(Uri url) async {
    return _MockHttpClientRequest();
  }

  @override
  Future<HttpClientRequest> head(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> headUrl(Uri url) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> open(String method, String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> openUrl(String method, Uri url) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> patch(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> patchUrl(Uri url) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> post(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> postUrl(Uri url) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> put(String host, int port, String path) => throw UnimplementedError();

  @override
  Future<HttpClientRequest> putUrl(Uri url) => throw UnimplementedError();

  @override
  set keyLog(void Function(String line)? callback) {}
}

class _MockHttpClientRequest implements HttpClientRequest {
  @override
  Encoding get encoding => throw UnimplementedError();
  @override
  set encoding(Encoding value) => throw UnimplementedError();
  @override
  int get contentLength => throw UnimplementedError();
  @override
  set contentLength(int value) => throw UnimplementedError();
  @override
  bool get bufferOutput => throw UnimplementedError();
  @override
  set bufferOutput(bool value) => throw UnimplementedError();
  @override
  bool get followRedirects => throw UnimplementedError();
  @override
  set followRedirects(bool value) => throw UnimplementedError();
  @override
  int get maxRedirects => throw UnimplementedError();
  @override
  set maxRedirects(int value) => throw UnimplementedError();
  @override
  bool get persistentConnection => throw UnimplementedError();
  @override
  set persistentConnection(bool value) => throw UnimplementedError();

  @override
  void abort([Object? exception, StackTrace? stackTrace]) {}
  @override
  void add(List<int> data) {}
  @override
  void addError(Object error, [StackTrace? stackTrace]) {}
  @override
  Future<void> addStream(Stream<List<int>> stream) async {}
  @override
  Future<HttpClientResponse> close() async => _MockHttpClientResponse();
  @override
  HttpConnectionInfo? get connectionInfo => null;
  @override
  List<Cookie> get cookies => [];
  @override
  Future<HttpClientResponse> get done => close();
  @override
  Future<void> flush() async {}
  @override
  HttpHeaders get headers => _MockHttpHeaders();
  @override
  String get method => 'GET';
  @override
  Uri get uri => Uri.parse('http://mock');
  @override
  void write(Object? object) {}
  @override
  void writeAll(Iterable objects, [String separator = ""]) {}
  @override
  void writeCharCode(int charCode) {}
  @override
  void writeln([Object? object = ""]) {}
}

class _MockHttpClientResponse implements HttpClientResponse {
  @override
  int get statusCode => 404;
  @override
  String get reasonPhrase => 'Not Found';
  @override
  int get contentLength => 0;
  @override
  HttpClientResponseCompressionState get compressionState => HttpClientResponseCompressionState.notCompressed;
  @override
  HttpConnectionInfo? get connectionInfo => null;
  @override
  List<Cookie> get cookies => [];
  @override
  HttpHeaders get headers => _MockHttpHeaders();
  @override
  Future<Socket> detachSocket() => throw UnimplementedError();

  @override
  bool get isRedirect => false;
  @override
  bool get persistentConnection => false;
  @override
  Future<HttpClientResponse> redirect([String? method, Uri? url, bool? followLoops]) => throw UnimplementedError();
  @override
  List<RedirectInfo> get redirects => [];

  // Stream implementation fixes using Stream<S>
  @override
  Stream<S> expand<S>(Iterable<S> Function(List<int> element) convert) => const Stream.empty();
  @override
  Future<String> join([String separator = ""]) async => "";
  @override
  Future<List<int>> lastWhere(bool Function(List<int> element) test, {List<int> Function()? orElse}) => throw UnimplementedError();
  @override
  Future<List<int>> reduce(List<int> Function(List<int> previous, List<int> element) combine) => throw UnimplementedError();
  @override
  Future<List<int>> get single => throw UnimplementedError();
  @override
  Future<List<int>> singleWhere(bool Function(List<int> element) test, {List<int> Function()? orElse}) => throw UnimplementedError();
  @override
  Future<List<List<int>>> toList() async => [];
  @override
  Future<Set<List<int>>> toSet() async => {};

  @override
  StreamSubscription<List<int>> listen(void Function(List<int> event)? onData, {Function? onError, void Function()? onDone, bool? cancelOnError}) {
    return const Stream<List<int>>.empty().listen(onData, onError: onError, onDone: onDone, cancelOnError: cancelOnError);
  }

  @override
  Future<bool> any(bool Function(List<int> element) test) async => false;
  @override
  Stream<List<int>> asBroadcastStream({void Function(StreamSubscription<List<int>> subscription)? onListen, void Function(StreamSubscription<List<int>> subscription)? onCancel}) => const Stream<List<int>>.empty();
  @override
  Stream<E> asyncExpand<E>(Stream<E>? Function(List<int> event) convert) => const Stream.empty();
  @override
  Stream<E> asyncMap<E>(FutureOr<E> Function(List<int> event) convert) => const Stream.empty();
  @override
  Stream<R> cast<R>() => const Stream.empty();
  @override
  Future<bool> contains(Object? needle) async => false;
  @override
  Stream<List<int>> distinct([bool Function(List<int> previous, List<int> next)? equals]) => const Stream.empty();
  @override
  Future<E> drain<E>([E? futureValue]) async => futureValue as E;
  @override
  Future<List<int>> elementAt(int index) => throw UnimplementedError();
  @override
  Future<bool> every(bool Function(List<int> element) test) async => true;
  @override
  Future<List<int>> get first => throw UnimplementedError();
  @override
  Future<List<int>> firstWhere(bool Function(List<int> element) test, {List<int> Function()? orElse}) => throw UnimplementedError();
  @override
  Future<S> fold<S>(S initialValue, S Function(S previous, List<int> element) combine) async => initialValue;
  @override
  Future<void> forEach(void Function(List<int> element) action) async {}
  @override
  Stream<List<int>> handleError(Function onError, {bool Function(dynamic error)? test}) => const Stream.empty();
  @override
  bool get isBroadcast => false;
  @override
  Future<bool> get isEmpty async => true;
  @override
  Future<List<int>> get last => throw UnimplementedError();
  @override
  Future<int> get length async => 0;
  @override
  Stream<S> map<S>(S Function(List<int> event) convert) => const Stream.empty();
  @override
  Future pipe(StreamConsumer<List<int>> streamConsumer) => streamConsumer.addStream(this);
  @override
  Stream<List<int>> skip(int count) => const Stream.empty();
  @override
  Stream<List<int>> skipWhile(bool Function(List<int> element) test) => const Stream.empty();
  @override
  Stream<List<int>> take(int count) => const Stream.empty();
  @override
  Stream<List<int>> takeWhile(bool Function(List<int> element) test) => const Stream.empty();
  @override
  Stream<List<int>> timeout(Duration timeLimit, {void Function(EventSink<List<int>> sink)? onTimeout}) => const Stream.empty();
  @override
  Stream<S> transform<S>(StreamTransformer<List<int>, S> streamTransformer) => streamTransformer.bind(this);
  @override
  Stream<List<int>> where(bool Function(List<int> event) test) => const Stream.empty();

  @override
  X509Certificate? get certificate => null;
}

class _MockHttpHeaders implements HttpHeaders {
  @override
  bool chunkedTransferEncoding = false;
  @override
  int contentLength = 0;
  @override
  ContentType? contentType;
  @override
  DateTime? date;
  @override
  DateTime? expires;
  @override
  String? host;
  @override
  DateTime? ifModifiedSince;
  @override
  bool persistentConnection = false;
  @override
  int? port;

  @override
  List<String>? operator [](String name) => [];
  @override
  void add(String name, Object value, {bool preserveHeaderCase = false}) {}
  @override
  void clear() {}
  @override
  void forEach(void Function(String name, List<String> values) action) {}
  @override
  void noFolding(String name) {}
  @override
  void remove(String name, Object value) {}
  @override
  void removeAll(String name) {}
  @override
  void set(String name, Object value, {bool preserveHeaderCase = false}) {}
  @override
  String? value(String name) => null;
}

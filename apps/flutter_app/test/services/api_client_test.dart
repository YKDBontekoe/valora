import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/mockito.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_client.dart';

// Manual Mock
class MockClient extends Mock implements http.Client {
  @override
  Future<http.Response> get(Uri? url, {Map<String, String>? headers}) async {
    return super.noSuchMethod(
      Invocation.method(#get, [url], {#headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }

    @override
  Future<http.Response> post(Uri? url, {Map<String, String>? headers, Object? body, Encoding? encoding}) async {
    return super.noSuchMethod(
      Invocation.method(#post, [url], {#headers: headers, #body: body, #encoding: encoding}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }
}

void main() {
  group('ApiClient', () {
    late MockClient mockHttpClient;
    late ApiClient apiClient;

    setUp(() {
      mockHttpClient = MockClient();
      apiClient = ApiClient(client: mockHttpClient);
    });

    test('handleResponse throws JsonParsingException on malformed JSON', () async {
      final response = http.Response('invalid json', 200);

      expect(
        () => apiClient.handleResponse(response, (body) => json.decode(body)),
        throwsA(isA<JsonParsingException>()),
      );
    });

    test('handleResponse returns parsed data on valid JSON', () async {
      final response = http.Response('{"key": "value"}', 200);

      final result = await apiClient.handleResponse(response, (body) => json.decode(body));
      expect(result, {'key': 'value'});
    });
  });
}

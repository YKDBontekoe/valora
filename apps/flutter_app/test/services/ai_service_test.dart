import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/services/ai_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

@GenerateNiceMocks([MockSpec<http.Client>()])
import 'ai_service_test.mocks.dart';

class MockAuthService extends Mock implements AuthService {
  @override
  Future<String?> getToken() async => 'fake-token';
}

void main() {
  late AiService aiService;
  late MockClient mockClient;
  late MockAuthService mockAuthService;

  setUp(() async {
    await dotenv.load(fileName: '.env.example');
    mockClient = MockClient();
    mockAuthService = MockAuthService();
    aiService = AiService(client: mockClient, authService: mockAuthService);
  });

  test('sendMessage sanitizes backend error details', () async {
    when(mockClient.post(
      any,
      headers: anyNamed('headers'),
      body: anyNamed('body'),
    )).thenAnswer((_) async => http.Response('{"detail": "Secret DB error", "trace_id": "12345"}', 500));

    expect(
      () => aiService.sendMessage(prompt: 'test'),
      throwsA(isA<Exception>().having((e) => e.toString(), 'message', contains('Failed to send message. (Trace ID: 12345)'))),
    );
  });

  test('sendMessage handles missing trace_id gracefully', () async {
    when(mockClient.post(
      any,
      headers: anyNamed('headers'),
      body: anyNamed('body'),
    )).thenAnswer((_) async => http.Response('{"detail": "Secret DB error"}', 500));

    expect(
      () => aiService.sendMessage(prompt: 'test'),
      throwsA(isA<Exception>().having((e) => e.toString(), 'message', isNot(contains('Trace ID:')))),
    );
  });
}

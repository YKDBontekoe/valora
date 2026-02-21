import 'package:mockito/mockito.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/support_status.dart';
import 'dart:async';

class MockApiService extends Mock implements ApiService {
  @override
  Future<SupportStatus> getSupportStatus() {
    return super.noSuchMethod(
      Invocation.method(#getSupportStatus, []),
      returnValue: Future.value(SupportStatus.fallback()),
      returnValueForMissingStub: Future.value(SupportStatus.fallback()),
    ) as Future<SupportStatus>;
  }
}

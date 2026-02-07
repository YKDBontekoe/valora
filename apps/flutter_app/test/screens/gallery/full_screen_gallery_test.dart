import 'dart:io';
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/screens/gallery/full_screen_gallery.dart';

class MockHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return _MockHttpClient();
  }
}

class _MockHttpClient extends Mock implements HttpClient {
  @override
  Future<HttpClientRequest> getUrl(Uri url) async => _MockHttpClientRequest();
}

class _MockHttpClientRequest extends Mock implements HttpClientRequest {
  @override
  Future<HttpClientResponse> close() async => _MockHttpClientResponse();
}

class _MockHttpClientResponse extends Mock implements HttpClientResponse {
  final List<int> _imageBytes = const [
    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
    0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
    0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
    0x42, 0x60, 0x82
  ];

  @override
  int get statusCode => 200;

  @override
  int get contentLength => _imageBytes.length;

  @override
  HttpClientResponseCompressionState get compressionState => HttpClientResponseCompressionState.notCompressed;

  @override
  StreamSubscription<List<int>> listen(void Function(List<int> event)? onData,
      {Function? onError, void Function()? onDone, bool? cancelOnError}) {
    onData?.call(_imageBytes);
    onDone?.call();
    return Stream<List<int>>.fromIterable([_imageBytes]).listen(null);
  }
}

void main() {
  setUp(() {
    HttpOverrides.global = MockHttpOverrides();
  });

  testWidgets('FullScreenGallery shows initial counter', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: FullScreenGallery(
          imageUrls: ['https://img.test/1.png', 'https://img.test/2.png'],
          initialIndex: 1,
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));

    expect(find.text('2 / 2'), findsOneWidget);
    expect(find.byType(InteractiveViewer), findsOneWidget);
  });

  testWidgets('FullScreenGallery close button pops the route', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) {
            return Scaffold(
              body: Center(
                child: ElevatedButton(
                  onPressed: () => Navigator.of(context).push(
                    MaterialPageRoute<void>(
                      builder: (_) => const FullScreenGallery(
                        imageUrls: ['https://img.test/1.png'],
                      ),
                    ),
                  ),
                  child: const Text('Open'),
                ),
              ),
            );
          },
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));
    expect(find.byType(FullScreenGallery), findsOneWidget);

    final closeButton = find.descendant(
      of: find.byType(FullScreenGallery),
      matching: find.byIcon(Icons.close_rounded),
    );
    await tester.tapAt(tester.getCenter(closeButton));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    expect(find.byType(FullScreenGallery), findsNothing);
    expect(find.text('Open'), findsOneWidget);
  });

  testWidgets('FullScreenGallery clamps out-of-range initialIndex', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: FullScreenGallery(
          imageUrls: ['https://img.test/1.png', 'https://img.test/2.png'],
          initialIndex: 99,
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));

    expect(find.text('2 / 2'), findsOneWidget);
  });
}

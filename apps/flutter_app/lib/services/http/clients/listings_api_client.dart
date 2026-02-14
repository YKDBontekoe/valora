import '../../../core/exceptions/app_exceptions.dart';
import '../../../models/listing.dart';
import '../../../models/listing_filter.dart';
import '../../../models/listing_response.dart';
import '../core/api_runner.dart';
import '../core/http_transport.dart';
import '../mappers/listings_api_mapper.dart';

class ListingsApiClient {
  ListingsApiClient({
    required HttpTransport transport,
    required ApiRunner runner,
  })  : _transport = transport,
        _runner = runner;

  final HttpTransport _transport;
  final ApiRunner _runner;

  Future<ListingResponse> getListings(
    ListingFilter filter, {
    int page = 1,
    int pageSize = 20,
  }) async {
    final Uri uri = Uri.parse('${_transport.baseUrl}/listings').replace(
      queryParameters: <String, String>{
        'page': page.toString(),
        'pageSize': pageSize.toString(),
        ...filter.toQueryParameters(),
      },
    );

    return _transport.get<ListingResponse>(
      uri: uri,
      retryOnNetworkError: true,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        (body) => _runner(ListingsApiMapper.parseListingResponse, body),
      ),
    );
  }

  Future<Listing> getListing(String id) async {
    final String sanitizedId = _sanitizeListingId(id);
    final Uri uri = Uri.parse('${_transport.baseUrl}/listings/$sanitizedId');

    return _transport.get<Listing>(
      uri: uri,
      retryOnNetworkError: true,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        (body) => _runner(ListingsApiMapper.parseListing, body),
      ),
    );
  }

  Future<Listing?> getListingFromPdok(String id) async {
    final Uri uri = Uri.parse('${_transport.baseUrl}/listings/lookup').replace(
      queryParameters: <String, String>{'id': id},
    );

    try {
      return await _transport.get<Listing>(
        uri: uri,
        responseHandler: (response) => _transport.parseOrThrow(
          response,
          (body) => _runner(ListingsApiMapper.parseListing, body),
        ),
      );
    } on NotFoundException {
      return null;
    }
  }

  Future<bool> healthCheck() async {
    final Uri uri = Uri.parse('${_transport.baseUrl}/health');
    try {
      return await _transport.get<bool>(
        uri: uri,
        responseHandler: (response) => response.statusCode == 200,
      );
    } catch (_) {
      return false;
    }
  }

  String _sanitizeListingId(String id) {
    final String sanitized = id.trim();
    if (sanitized.isEmpty || sanitized.contains(RegExp(r'[/?#]'))) {
      throw ValidationException('Invalid listing identifier');
    }
    return sanitized;
  }
}

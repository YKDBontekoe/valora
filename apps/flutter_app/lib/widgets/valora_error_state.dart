import 'package:flutter/material.dart';
import '../core/exceptions/app_exceptions.dart';
import 'valora_widgets.dart';

class ValoraErrorState extends StatelessWidget {
  final Object error;
  final VoidCallback onRetry;

  const ValoraErrorState({
    super.key,
    required this.error,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    IconData icon = Icons.error_outline;
    String title = 'Something went wrong';
    String message = 'An unexpected error occurred. Please try again.';

    if (error is NetworkException) {
      icon = Icons.wifi_off_rounded;
      title = 'No Connection';
      message = (error as NetworkException).message;
    } else if (error is ServerException) {
      icon = Icons.cloud_off_rounded;
      title = 'Server Error';
      message = (error as ServerException).message;
    } else if (error is NotFoundException) {
      icon = Icons.search_off_rounded;
      title = 'Not Found';
      message = (error as NotFoundException).message;
    } else if (error is ValidationException) {
      icon = Icons.warning_amber_rounded;
      title = 'Invalid Request';
      message = (error as ValidationException).message;
    } else if (error is AppException) {
      message = (error as AppException).message;
    }

    return ValoraEmptyState(
      icon: icon,
      title: title,
      subtitle: message,
      actionLabel: 'Try Again',
      onAction: onRetry,
    );
  }
}

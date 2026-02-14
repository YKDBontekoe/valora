import 'package:flutter/foundation.dart';

typedef ApiRunner = Future<R> Function<Q, R>(
  ComputeCallback<Q, R> callback,
  Q message, {
  String? debugLabel,
});

Future<R> defaultApiRunner<Q, R>(
  ComputeCallback<Q, R> callback,
  Q message, {
  String? debugLabel,
}) async {
  return callback(message);
}

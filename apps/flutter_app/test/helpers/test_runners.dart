import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:valora_app/services/api_service.dart';

Future<R> syncRunner<Q, R>(ComputeCallback<Q, R> callback, Q message, {String? debugLabel}) async =>
    await callback(message);

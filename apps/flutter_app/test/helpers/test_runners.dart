import 'dart:async';
import 'package:valora_app/services/api_service.dart';

Future<R> syncRunner<Q, R>(ComputeCallback<Q, R> callback, Q message) async => await callback(message);

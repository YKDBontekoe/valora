import 'dart:async';
import 'package:flutter/foundation.dart';

Future<R> syncRunner<Q, R>(ComputeCallback<Q, R> callback, Q message, {String? debugLabel}) async =>
    await callback(message);

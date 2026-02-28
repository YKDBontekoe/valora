import 'package:flutter/foundation.dart';
import '../models/ai_chat_message.dart';
import '../models/ai_conversation.dart';
import '../services/ai_service.dart';

class AiChatProvider with ChangeNotifier {
  AiService _aiService;

  AiChatProvider(this._aiService);

  void updateService(AiService service) {
    _aiService = service;
  }

  List<AiConversation> _conversations = [];
  List<AiConversation> get conversations => _conversations;

  String? _activeConversationId;
  String? get activeConversationId => _activeConversationId;

  List<AiChatMessage> _activeMessages = [];
  List<AiChatMessage> get activeMessages => _activeMessages;

  bool _isLoadingHistory = false;
  bool get isLoadingHistory => _isLoadingHistory;

  bool _isSending = false;
  bool get isSending => _isSending;

  String? _error;
  String? get error => _error;

  String? _lastFailedPrompt;
  String? get lastFailedPrompt => _lastFailedPrompt;

  Map<String, dynamic>? _currentContextReport;

  void setContextReport(Map<String, dynamic>? report) {
    _currentContextReport = report;
    notifyListeners();
  }

  Future<void> loadHistory() async {
    _isLoadingHistory = true;
    _error = null;
    _lastFailedPrompt = null;
    notifyListeners();

    try {
      _conversations = await _aiService.getHistory();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoadingHistory = false;
      notifyListeners();
    }
  }

  Future<void> loadConversation(String conversationId) async {
    _activeConversationId = conversationId;
    _activeMessages = [];
    _error = null;
    _lastFailedPrompt = null;
    notifyListeners();

    try {
      _activeMessages = await _aiService.getMessages(conversationId);
    } catch (e) {
      _error = e.toString();
    } finally {
      notifyListeners();
    }
  }

  void startNewConversation() {
    _activeConversationId = null;
    _activeMessages = [];
    _currentContextReport = null;
    _error = null;
    _lastFailedPrompt = null;
    notifyListeners();
  }

  Future<void> deleteConversation(String conversationId) async {
    try {
      await _aiService.deleteConversation(conversationId);
      _conversations.removeWhere((c) => c.id == conversationId);
      if (_activeConversationId == conversationId) {
        startNewConversation();
      } else {
        notifyListeners();
      }
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  Future<void> sendMessage(String prompt) async {
    if (prompt.trim().isEmpty) return;

    _error = null;
    _lastFailedPrompt = null;
    _isSending = true;

    // Optimistically add user message if not already present (e.g. from retry)
    final existingIndex = _activeMessages.isEmpty 
        ? -1 
        : _activeMessages.indexWhere((m) => m.role == 'user' && m.content == prompt && m == _activeMessages.last);
    
    if (existingIndex == -1) {
      final userMessage = AiChatMessage(
        role: 'user',
        content: prompt,
        createdAtUtc: DateTime.now().toUtc(),
      );
      _activeMessages.add(userMessage);
    }
    
    notifyListeners();

    try {
      final response = await _aiService.sendMessage(
        conversationId: _activeConversationId,
        prompt: prompt,
        // Send previous messages as history
        history: _activeMessages.length > 1 
            ? _activeMessages.take(_activeMessages.length - 1).toList() 
            : null,
        contextReport: _currentContextReport,
      );

      final assistantMessage = AiChatMessage(
        role: 'assistant',
        content: response['response'] ?? '',
        createdAtUtc: DateTime.now().toUtc(),
      );

      _activeMessages.add(assistantMessage);

      if (_activeConversationId == null && response['conversationId'] != null) {
        _activeConversationId = response['conversationId'];
        loadHistory();
      }
    } catch (e) {
      _error = e.toString();
      _lastFailedPrompt = prompt;
      // We keep the message in _activeMessages so UI can show error state on it
    } finally {
      _isSending = false;
      notifyListeners();
    }
  }

  void retryLastMessage() {
    if (_lastFailedPrompt != null) {
      final prompt = _lastFailedPrompt!;
      sendMessage(prompt);
    }
  }
}

class User {
  final String id;
  final String email;
  final List<String> preferredCities;

  User({
    required this.id,
    required this.email,
    required this.preferredCities,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['userId'] ?? json['id'] ?? '',
      email: json['email'] ?? '',
      preferredCities: json['preferredCities'] != null
          ? List<String>.from(json['preferredCities'])
          : [],
    );
  }
}

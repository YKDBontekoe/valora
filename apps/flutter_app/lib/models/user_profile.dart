class UserProfile {
  final String email;
  final String? firstName;
  final String? lastName;
  final int defaultRadiusMeters;
  final bool biometricsEnabled;

  UserProfile({
    required this.email,
    this.firstName,
    this.lastName,
    required this.defaultRadiusMeters,
    required this.biometricsEnabled,
  });

  String get displayName {
    if (firstName != null && lastName != null) {
      return '$firstName $lastName';
    }
    return firstName ?? lastName ?? email;
  }

  String get initials {
    if (firstName != null && firstName!.isNotEmpty && lastName != null && lastName!.isNotEmpty) {
      return '${firstName![0]}${lastName![0]}'.toUpperCase();
    }
    if (firstName != null && firstName!.isNotEmpty) {
      return firstName![0].toUpperCase();
    }
    return email.substring(0, email.length >= 2 ? 2 : 1).toUpperCase();
  }

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      email: json['email'],
      firstName: json['firstName'],
      lastName: json['lastName'],
      defaultRadiusMeters: json['defaultRadiusMeters'],
      biometricsEnabled: json['biometricsEnabled'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'defaultRadiusMeters': defaultRadiusMeters,
      'biometricsEnabled': biometricsEnabled,
    };
  }
}

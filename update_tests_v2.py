import re

files = [
    'backend/Valora.UnitTests/Services/ContextReportServiceTests.cs',
    'backend/Valora.UnitTests/Services/ContextReportScoringTests.cs'
]

for file_path in files:
    with open(file_path, 'r') as f:
        content = f.read()

    # 1. Add fields
    fields_match = 'private readonly Mock<IAirQualityClient> _airClient;'
    fields_injection = """private readonly Mock<IAirQualityClient> _airClient;
    private readonly Mock<IPdokSoilClient> _soilClient;
    private readonly Mock<IPdokBuildingClient> _buildingClient;"""
    content = content.replace(fields_match, fields_injection)

    # 2. Initialize in Constructor
    ctor_match = '_airClient = new Mock<IAirQualityClient>();'
    ctor_injection = """_airClient = new Mock<IAirQualityClient>();
        _soilClient = new Mock<IPdokSoilClient>();
        _buildingClient = new Mock<IPdokBuildingClient>();"""
    content = content.replace(ctor_match, ctor_injection)

    # 3. Update CreateService
    create_match = '_airClient.Object,'
    create_injection = """_airClient.Object,
            _soilClient.Object,
            _buildingClient.Object,"""
    content = content.replace(create_match, create_injection)

    # 4. Update SetupDefaults (ScoringTests only)
    if 'SetupDefaults' in content:
        setup_match = '_airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))\n            .ReturnsAsync((AirQualitySnapshotDto?)null);'
        setup_injection = """_airClient.Setup(x => x.GetSnapshotAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualitySnapshotDto?)null);
        _soilClient.Setup(x => x.GetFoundationRiskAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoundationRiskDto?)null);
        _buildingClient.Setup(x => x.GetSolarPotentialAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SolarPotentialDto?)null);"""
        content = content.replace(setup_match, setup_injection)

    # 5. Add New Tests to ScoringTests
    if 'ScorePm25_ReturnsCorrectScore' in content:
        last_test_anchor = """    public async Task ScorePm25_ReturnsCorrectScore(double pm25, double expectedScore)
    {
        SetupDefaults(_location);

        _airClient.Setup(x => x.GetSnapshotAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto("ID", "Name", 100, pm25, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.EnvironmentMetrics.Single(m => m.Key == "pm25");
        Assert.Equal(expectedScore, metric.Score);
    }"""

        new_tests = """
    [Theory]
    [InlineData("Low", 100)]
    [InlineData("Medium", 60)]
    [InlineData("High", 30)]
    public async Task ScoreFoundation_ReturnsCorrectScore(string risk, double expectedScore)
    {
        SetupDefaults(_location);

        _soilClient.Setup(x => x.GetFoundationRiskAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoundationRiskDto(risk, "Sand", "Desc", DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.HousingMetrics.Single(m => m.Key == "foundation_risk");
        Assert.Equal(expectedScore, metric.Score);
    }

    [Theory]
    [InlineData("High", 100)]
    [InlineData("Medium", 75)]
    [InlineData("Low", 50)]
    public async Task ScoreSolar_ReturnsCorrectScore(string potential, double expectedScore)
    {
        SetupDefaults(_location);

        _buildingClient.Setup(x => x.GetSolarPotentialAsync(_location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolarPotentialDto(potential, 50, 10, 3000, DateTimeOffset.UtcNow));

        var service = CreateService();
        var report = await service.BuildAsync(new ContextReportRequestDto("test"));

        var metric = report.EnvironmentMetrics.Single(m => m.Key == "solar_potential");
        Assert.Equal(expectedScore, metric.Score);
    }"""
        content = content.replace(last_test_anchor, last_test_anchor + new_tests)

    with open(file_path, 'w') as f:
        f.write(content)

print("Tests updated successfully.")

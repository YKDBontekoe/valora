namespace Valora.Domain.Common;

public static class ValidationConstants
{
    public static class Listing
    {
        public const int AddressMaxLength = 200;
        public const int CityMaxLength = 100;
        public const int PostalCodeMaxLength = 20;
        public const int UrlMaxLength = 500;
        public const int ImageUrlMaxLength = 500;
        public const int PropertyTypeMaxLength = 100;
        public const int StatusMaxLength = 50;
        public const int EnergyLabelMaxLength = 20;
        public const int OwnershipTypeMaxLength = 100;
        public const int CadastralDesignationMaxLength = 100;
        public const int HeatingTypeMaxLength = 100;
        public const int InsulationTypeMaxLength = 100;
        public const int GardenOrientationMaxLength = 50;
        public const int ParkingTypeMaxLength = 100;
        public const int AgentNameMaxLength = 200;
        public const int RoofTypeMaxLength = 100;
        public const int ConstructionPeriodMaxLength = 100;
        public const int CVBoilerBrandMaxLength = 100;
        public const int BrokerPhoneMaxLength = 50;
        public const int BrokerAssociationCodeMaxLength = 20;
        public const int FundaIdMaxLength = 50;
    }

    public static class Notification
    {
        public const int UserIdMaxLength = 450;
        public const int TitleMaxLength = 200;
        public const int BodyMaxLength = 2000;
        public const int ActionUrlMaxLength = 500;
    }
}

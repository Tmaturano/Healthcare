namespace Healthcare.Application.DTOs;

public record AdherenceScoreResponse(
    Guid PatientId,
    double Score,
    DateTime CalculatedAt,
    string Description
);

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;

public record GetAllReportsQuery();
public record GetReportsByReportedUserIdQuery(int ReportedUserId);
public record GetConfirmedReportsByReportedUserIdQuery(int ReportedUserId);
public record CountConfirmedReportsByReportedUserIdQuery(int ReportedUserId);

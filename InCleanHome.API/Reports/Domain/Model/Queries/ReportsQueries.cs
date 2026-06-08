namespace InCleanHome.API.Reports.Domain.Model.Queries;

public record GetAllReportsQuery();
public record GetReportsByReportedUserIdQuery(int ReportedUserId);
public record GetConfirmedReportsByReportedUserIdQuery(int ReportedUserId);
public record CountConfirmedReportsByReportedUserIdQuery(int ReportedUserId);

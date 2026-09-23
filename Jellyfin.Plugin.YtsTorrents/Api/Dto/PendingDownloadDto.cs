using System;

namespace Jellyfin.Plugin.YtsTorrents.Api.Dto;

public record PendingDownloadDto(
    string Hash,
    string MovieTitle,
    int Year,
    string State,
    string? LastError,
    DateTime AddedUtc,
    double Progress,
    string? QbState,
    long? DownloadSpeed = null,
    long? EstimatedTimeRemaining = null,
    long? UploadedBytes = null);

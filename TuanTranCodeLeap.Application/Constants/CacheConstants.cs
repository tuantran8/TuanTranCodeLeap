namespace TuanTranCodeLeap.Application.Constants;

public static class CacheConstants
{
    public const int SearchCacheAbsoluteExpirationMinutes = 5;
    public const int SearchCacheSlidingExpirationMinutes = 2;
    public const string SearchCacheKeyPrefix = "search_products";
    public const string SearchCacheVersionKey = "search_products_version";
}

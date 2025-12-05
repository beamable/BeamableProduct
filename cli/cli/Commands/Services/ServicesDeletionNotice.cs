namespace cli;

public static class ServicesDeletionNotice
{
    
    public const string REMOVED_PREFIX = "[REMOVED] ";
    public const string TITLE = "[red]This command has been removed[/]\nIn Beam CLI6+, the 'beam services' command suite has been made obsolete.\n";
    public const string UNSUPPORTED_MESSAGE 
        = 
        "This command has no forward support. ";
    public const string RUN_MESSAGE 
        = 
        "`beam services run` would build and run your services in Docker. If you just want to " +
        "run your services, then use the `beam project run` command, which will run the services " +
        "locally on your computer without Docker. \n" +
        "If you want to validate your Docker images, use \n" +
        "> `beam deploy plan --docker-compose-dir example', " +
        "which will create a full docker compose project in the 'example' directory. You can use " +
        "`docker compose` to run the images. ";
    
    public const string STOP_MESSAGE 
        = 
        "Please use `beam project stop` instead. ";


    public const string BUILD_MESSAGE
        =
        "`beam services build` would build your services as Docker images. Instead, please use the following command, " +
        "> `beam deploy plan` " +
        "This will create Docker images on your computer you can inspect with `docker images ps`. ";

    public const string LIST_MESSAGE
        =
        "Please use `beam project ps` instead. ";
    
    public const string MANIFEST_MESSAGE
        =
        "Please use `beam deploy list` instead. ";
}
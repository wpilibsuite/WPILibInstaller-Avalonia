public class ToolConfig {
  public String name;
  public String version;
  public ArtifactConfig artifact;
  public Boolean cpp;

  public boolean isValid() {
    return name != null && version != null;
  }
}

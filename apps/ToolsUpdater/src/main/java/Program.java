import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.Arrays;

import com.google.gson.Gson;

public class Program {
  public static void main(String[] args) throws URISyntaxException, IOException {
    Gson gson = new Gson();

    File toolsDir = new File(Program.class.getProtectionDomain().getCodeSource().getLocation().toURI()).getParentFile();

    File homeDir = toolsDir.getParentFile();

    String toolsPath = toolsDir.getAbsolutePath();

    File jsonFile = new File(toolsDir, "tools.json");

    final String mavenDir = new File(homeDir, "maven").getPath();

    try (FileReader reader = new FileReader(jsonFile)) {
      ToolConfig[] tools = gson.fromJson(reader, ToolConfig[].class);
      Arrays.stream(tools).filter(x -> x.isValid()).forEach(tool -> {
        ArtifactConfig artifact = tool.artifact;
        String artifactFileName = artifact.artifactId + '-' + artifact.version;
        if (artifact.classifier != null && !artifact.classifier.isBlank()) {
          artifactFileName += '-' + artifact.classifier;
        }
        artifactFileName += '.' + artifact.extension;

        Path artifactPath = Paths.get(mavenDir, artifact.groupId.replace('.', File.separatorChar), artifact.artifactId, artifact.version, artifactFileName);
        if (artifactPath.toFile().exists()) {
          try {
            Files.copy(artifactPath, Paths.get(toolsPath, tool.name + ".jar"), StandardCopyOption.REPLACE_EXISTING);
            Files.copy(Paths.get(toolsPath, "ScriptBase.vbs"), Paths.get(toolsPath, tool.name + ".vbs"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
            Files.copy(Paths.get(toolsPath, "ScriptBase.py"), Paths.get(toolsPath, tool.name + ".py"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
          } catch (IOException e) {
            System.out.println(e.toString());
            e.printStackTrace();
          }
        }
      });
    }
  }
}

import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.Arrays;
import net.lingala.zip4j.ZipFile;

import com.google.gson.Gson;

import org.apache.commons.io.FileUtils;
import org.apache.commons.lang3.SystemUtils;

public class Program {
  private static void installJavaTool(ToolConfig tool, String toolsPath) {
    ArtifactConfig artifact = tool.artifact;
    String artifactFileName = artifact.artifactId + '-' + artifact.version;
    if (artifact.classifier != null && !artifact.classifier.isBlank()) {
      artifactFileName += '-' + artifact.classifier;
    }
    artifactFileName += '.' + artifact.extension;

    Path artifactPath = Paths.get(toolsPath, "artifacts", artifactFileName);
    System.out.println(artifactPath);
    if (artifactPath.toFile().exists()) {
      try {
        Files.copy(artifactPath, Paths.get(toolsPath, tool.name + ".jar"), StandardCopyOption.REPLACE_EXISTING);
        Files.copy(Paths.get(toolsPath, "ScriptBase.vbs"), Paths.get(toolsPath, tool.name + ".vbs"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
        Files.copy(Paths.get(toolsPath, "ScriptBase.py"), Paths.get(toolsPath, tool.name + ".py"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
        Files.copy(Paths.get(toolsPath, "ScriptBase.sh"), Paths.get(toolsPath, tool.name + ".sh"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
      } catch (IOException e) {
        System.out.println(e.toString());
        e.printStackTrace();
      }
    }
  }

  private static final String arm64arch = "arm64";
  private static final String x64arch = "x86-64";
  private static final String x86arch = "x86";

  private static String desktopArch() {
    if (SystemUtils.IS_OS_MAC) {
      return "universal";
    }
    String arch = System.getProperty("os.arch");
    if (arch.equals("arm64") || arch.equals("aarch64")) {
      return arm64arch;
    }
    return (arch.equals("amd64") || arch.equals("x86_64")) ? x64arch : x86arch;
  }

  private static String desktopOS() {
    return SystemUtils.IS_OS_WINDOWS ? "windows" : SystemUtils.IS_OS_MAC ? "osx" : "linux";
  }

  private static String getPlatformPath() {
    return desktopOS() + "/" + desktopArch();
  }

  private static void installCppTool(ToolConfig tool, String toolsPath) {
    ArtifactConfig artifact = tool.artifact;
    String artifactFileName = artifact.artifactId + '-' + artifact.version;
    artifactFileName += '-' + artifact.classifier;
    artifactFileName += '.' + artifact.extension;

    Path artifactPath = Paths.get(toolsPath, "artifacts", artifactFileName);
    if (artifactPath.toFile().exists()) {
      try {

        File tempDir = new File(toolsPath, "tmp");
        new ZipFile(artifactPath.toFile()).extractAll(tempDir.getAbsolutePath());

        // Find glass folder
        File exeFolder = new File(tempDir, getPlatformPath());

        FileUtils.copyDirectory(exeFolder, new File(toolsPath));

        FileUtils.deleteDirectory(tempDir);

        Files.copy(Paths.get(toolsPath, "ScriptBaseCpp.vbs"), Paths.get(toolsPath, tool.name + ".vbs"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
        Files.copy(Paths.get(toolsPath, "ScriptBaseCpp.py"), Paths.get(toolsPath, tool.name + ".py"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
        Files.copy(Paths.get(toolsPath, "ScriptBaseCpp.sh"), Paths.get(toolsPath, tool.name + ".sh"), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.COPY_ATTRIBUTES);
      } catch (IOException e) {
        System.out.println(e.toString());
        e.printStackTrace();
      }
    }
  }

  private static void installAdvantageScope(String toolsPath) {
    if (SystemUtils.IS_OS_MAC) {
      String arch = System.getProperty("os.arch");
      boolean isArm = arch.equals("arm64") || arch.equals("aarch64");
      String archiveFileName = "advantagescope-wpilib-mac-" +
          (isArm ? "arm64" : "x64") +
          ".tar.gz";
      String advantageScopeFolder = Paths.get(new File(toolsPath).getParent(), "advantagescope").toString();
      Path archivePath = Paths.get(advantageScopeFolder, archiveFileName);

      try {
        Runtime.getRuntime().exec(new String[] {
            "tar", "-xzf", archivePath.toString(), "-C", advantageScopeFolder
        }).waitFor();
      } catch (IOException | InterruptedException e) {
        System.out.println(e.toString());
        e.printStackTrace();
      }
    }
  }

  public static void main(String[] args) throws URISyntaxException, IOException {
    Gson gson = new Gson();

    File toolsDir = new File(Program.class.getProtectionDomain().getCodeSource().getLocation().toURI()).getParentFile();

    String toolsPath = toolsDir.getAbsolutePath();

    File jsonFile = new File(toolsDir, "tools.json");

    try (FileReader reader = new FileReader(jsonFile)) {
      ToolConfig[] tools = gson.fromJson(reader, ToolConfig[].class);
      Arrays.stream(tools).filter(x -> x.isValid()).forEach(tool -> {
        System.out.println("Installing " + tool.name);
        if (tool.name.equals("AdvantageScope")) {
          installAdvantageScope(toolsPath);
        } else if (tool.artifact != null) {
          if (tool.cpp) {
            installCppTool(tool, toolsPath);
          } else {
            installJavaTool(tool, toolsPath);
          }
        }
      });
    }
  }
}

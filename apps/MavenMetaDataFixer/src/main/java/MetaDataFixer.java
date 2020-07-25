import java.io.File;
import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

import org.apache.maven.artifact.repository.metadata.Metadata;
import org.apache.maven.artifact.repository.metadata.Versioning;
import org.apache.maven.artifact.repository.metadata.io.xpp3.MetadataXpp3Reader;
import org.apache.maven.artifact.repository.metadata.io.xpp3.MetadataXpp3Writer;
import org.apache.maven.model.Model;
import org.apache.maven.model.io.xpp3.MavenXpp3Reader;
import org.codehaus.plexus.util.ReaderFactory;
import org.codehaus.plexus.util.WriterFactory;
import org.codehaus.plexus.util.xml.pull.XmlPullParserException;

public class MetaDataFixer {

  private static class Artifact {
    private final String groupId;
    private final String artifactId;

    public Artifact(String groupId, String artifactId) {
      this.groupId = groupId;
      this.artifactId = artifactId;
    }

    public String getGroupId() {
      return groupId;
    }

    public String getArtifactId() {
      return artifactId;
    }

    @Override
    public boolean equals(Object obj) {
      if (obj == this) {
        return true;
      }
      if (obj instanceof Artifact) {
        Artifact artif = (Artifact)obj;
        return Objects.equals(groupId, artif.groupId) && Objects.equals(artifactId, artif.artifactId);
      }
      return false;
    }

    @Override
    public int hashCode() {
      return Objects.hash(groupId, artifactId);
    }
  }

  private final Map<Artifact, List<String>> artifactStore = new LinkedHashMap<>();

  private final File pathRoot;

  public MetaDataFixer(File pathRoot) {
    this.pathRoot = pathRoot;
  }

  public void updateMetaData() {
    recurseMetaData(pathRoot);

    for (Map.Entry<Artifact, List<String>> entry : artifactStore.entrySet()) {

      Metadata metadata = new Metadata();
      Artifact key = entry.getKey();
      List<String> versions = entry.getValue();
      Collections.sort(versions);
      metadata.setGroupId(key.groupId);
      metadata.setArtifactId(key.artifactId);
      Versioning versioning = new Versioning();
      versioning.setRelease(versions.get(versions.size() - 1));
      for (String version : versions) {
        versioning.addVersion(version);
      }
      versioning.setLastUpdatedTimestamp(new Date());
      metadata.setVersioning(versioning);
      File writeFile = Paths.get(pathRoot.getPath(), key.getGroupId().replace('.', File.separatorChar), key.getArtifactId(), "maven-metadata.xml").toFile();
      try (Writer writer = WriterFactory.newXmlWriter(writeFile)) {
        new MetadataXpp3Writer().write(writer, metadata);
      } catch (IOException ex) {
        ex.printStackTrace();
      }
    }
  }

  private Model getMetadataForFile(File file) {
    try (Reader reader = ReaderFactory.newXmlReader(file)) {
      return new MavenXpp3Reader().read(reader);
    } catch (IOException e ) {
      e.printStackTrace();
    } catch (XmlPullParserException e) {
      e.printStackTrace();
    }
    return null;
  }

  private void recurseMetaData(File root) {
    File[] files = root.listFiles();
    for (File file : files) {
      if (file.isFile() && file.getName().endsWith(".pom")) {
        Model md = getMetadataForFile(file);
        if (md == null) {
          continue;
        }
        String groupId = md.getGroupId();
        if (groupId == null) {
          groupId = md.getParent().getGroupId();
        }
        String version = md.getVersion();
        if (version == null) {
          version = md.getParent().getVersion();
        }
        Artifact key = new Artifact(groupId, md.getArtifactId());
        List<String> versions = artifactStore.getOrDefault(key, null);
        if (versions != null) {
          versions.add(version);
        } else {
          versions = new ArrayList<>();
          versions.add(version);
          artifactStore.put(key, versions);
        }
      } else if (file.isDirectory()) {
        recurseMetaData(file);
      }
    }
  }
}

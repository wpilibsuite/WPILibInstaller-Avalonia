import java.io.File;
import java.io.IOException;
import java.io.Reader;
import java.net.URISyntaxException;

import org.apache.maven.artifact.repository.metadata.Metadata;
import org.apache.maven.artifact.repository.metadata.io.xpp3.MetadataXpp3Reader;
import org.codehaus.plexus.util.ReaderFactory;
import org.codehaus.plexus.util.xml.pull.XmlPullParserException;

public class Program {
  public static Metadata getMetadataForFile(File file) {
    try (Reader reader = ReaderFactory.newXmlReader(file)) {
      return new MetadataXpp3Reader().read(reader);
    } catch (IOException e ) {
    } catch (XmlPullParserException e) {
    }
    return null;
  }


  public static void main(String[] args) throws URISyntaxException {
    // Get root dir of files
    File rootDir = new File(Program.class.getProtectionDomain().getCodeSource().getLocation().toURI()).getParentFile();
    MetaDataFixer fixer = new MetaDataFixer(rootDir);
    fixer.updateMetaData();
  }
}

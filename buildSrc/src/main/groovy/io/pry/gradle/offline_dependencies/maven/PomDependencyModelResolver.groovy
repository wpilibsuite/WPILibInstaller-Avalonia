package io.pry.gradle.offline_dependencies.maven

import org.apache.maven.model.Parent
import org.apache.maven.model.Repository
import org.apache.maven.model.building.FileModelSource
import org.apache.maven.model.building.ModelSource
import org.apache.maven.model.resolution.InvalidRepositoryException
import org.apache.maven.model.resolution.ModelResolver
import org.apache.maven.model.resolution.UnresolvableModelException
import org.gradle.api.artifacts.result.UnresolvedArtifactResult
import org.gradle.api.artifacts.component.ModuleComponentIdentifier
import org.gradle.api.Project
import org.gradle.api.attributes.DocsType
import org.gradle.api.model.ObjectFactory
import org.gradle.internal.component.external.model.DefaultModuleComponentIdentifier
import org.gradle.api.internal.artifacts.DefaultModuleVersionIdentifier
import org.gradle.maven.MavenModule
import org.gradle.maven.MavenPomArtifact
import org.gradle.api.artifacts.Dependency

class PomDependencyModelResolver implements ModelResolver {

  def EMPTY_DEPENDENCIES_ARRAY = new Dependency[0]

  private Project project
  private Map<String, FileModelSource> pomCache = [:]
  private Map<String, FileModelSource> moduleCache = [:]
  private Map<ModuleComponentIdentifier, File> componentCache = [:]
  private Map<ModuleComponentIdentifier, File> moduleComponentCache = [:]

  public PomDependencyModelResolver(Project project) {
    this.project = project
  }

  @Override
  ModelSource resolveModel(Parent parent) throws UnresolvableModelException {
    return resolveModel(parent.groupId, parent.artifactId, parent.version)
  }

  @Override
  ModelSource resolveModel(String groupId, String artifactId, String version) throws UnresolvableModelException {
    def id = "$groupId:$artifactId:$version"

    if (!moduleCache.containsKey(id)) {
      def dep = project.dependencies.create(id)
      def cfg = project.configurations.detachedConfiguration([dep].toArray(EMPTY_DEPENDENCIES_ARRAY))

      def view = cfg.incoming.artifactView {
          withVariantReselection()
        lenient = true
        attributes {
          attribute(DocsType.DOCS_TYPE_ATTRIBUTE, project.objects.named(DocsType, "modules"))
        }
      }

      view.artifacts.each {
        def moduleFile = it.file
        def module = new FileModelSource(moduleFile)
        moduleCache[id] = module
        moduleComponentCache[it.variant.owner] = moduleFile
      }
    }

    if (!pomCache.containsKey(id)) {
      def mavenArtifacts = project.dependencies.createArtifactResolutionQuery()
          .forComponents(DefaultModuleComponentIdentifier.newId(new DefaultModuleVersionIdentifier(groupId, artifactId, version)))
          .withArtifacts(MavenModule, MavenPomArtifact)
          .execute()

      def component = mavenArtifacts.resolvedComponents.first()

      def poms = component.getArtifacts(MavenPomArtifact)
      if (poms?.empty) {
        return null
      }

      def pomArtifact = poms.first()

      if (pomArtifact instanceof UnresolvedArtifactResult) {
        logger.error("Resolver was unable to resolve artifact '{}'", pomArtifact.id, pomArtifact.getFailure())
        return null
      }

      def pomFile = pomArtifact.file as File

      def componentId = DefaultModuleComponentIdentifier.newId(new DefaultModuleVersionIdentifier(groupId, artifactId, version))
      componentCache[componentId] = pomFile

      def pom = new FileModelSource(pomFile)
      pomCache[id] = pom
      return pom
    }


    return pomCache[id]
  }

  @Override
  void addRepository(Repository repository, boolean replace) throws InvalidRepositoryException {}

  @Override
  void addRepository(Repository repository) throws InvalidRepositoryException {}

  @Override
  ModelResolver newCopy() { return this }

  public componentCache() {
    return this.componentCache
  }

  public moduleComponentCache() {
    return this.moduleComponentCache
  }
}

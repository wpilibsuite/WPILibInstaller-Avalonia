package io.pry.gradle.offline_dependencies.maven

import org.gradle.api.artifacts.ComponentMetadataContext
import org.gradle.api.artifacts.ComponentMetadataRule
import org.gradle.api.attributes.Category
import org.gradle.api.attributes.DocsType
import org.gradle.api.model.ObjectFactory

class DirectMetadataAccessVariantRule implements ComponentMetadataRule {
    @javax.inject.Inject
    ObjectFactory getObjects() { }
    void execute(ComponentMetadataContext context) {
        def id = context.details.id
        // context.details.addVariant("withPom") {
        //     attributes {
        //         attribute(Category.CATEGORY_ATTRIBUTE, objects.named(Category, Category.DOCUMENTATION))
        //         attribute(DocsType.DOCS_TYPE_ATTRIBUTE, objects.named(DocsType, "poms"))
        //     }
        //     withFiles {
        //         addFile("${id.name}-${id.version}.pom")
        //     }
        // }
        context.details.addVariant("withModules") {
            attributes {
                attribute(Category.CATEGORY_ATTRIBUTE, objects.named(Category, Category.DOCUMENTATION))
                attribute(DocsType.DOCS_TYPE_ATTRIBUTE, objects.named(DocsType, "modules"))
            }
            withFiles {
                removeAllFiles()
                addFile("${id.name}-${id.version}.module")
            }
        }
        // context.details.addVariant("withSources") {
        //     attributes {
        //         attribute(Category.CATEGORY_ATTRIBUTE, objects.named(Category, Category.DOCUMENTATION))
        //         attribute(DocsType.DOCS_TYPE_ATTRIBUTE, objects.named(DocsType, "srcs"))
        //     }
        //     withFiles {
        //         removeAllFiles()
        //         addFile("${id.name}-${id.version}-sources.jar")
        //     }
        // }
        // context.details.addVariant("withJavadoc") {
        //     attributes {
        //         attribute(Category.CATEGORY_ATTRIBUTE, objects.named(Category, Category.DOCUMENTATION))
        //         attribute(DocsType.DOCS_TYPE_ATTRIBUTE, objects.named(DocsType, "jdcs"))
        //     }
        //     withFiles {
        //         removeAllFiles()
        //         addFile("${id.name}-${id.version}-javadoc.jar")
        //     }
        // }
    }
}

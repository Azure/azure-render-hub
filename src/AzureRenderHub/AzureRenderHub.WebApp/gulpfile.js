
var gulp = require("gulp");
var sass = require("gulp-sass");

gulp.task("sass", function () {
    return gulp.src("Styles/main.scss")
        .pipe(sass().on("error", sass.logError))
        .pipe(gulp.dest("wwwroot/css"));
});

gulp.task('sass:watch', function () {
    gulp.watch("Styles/**/*.scss", ["sass"]);
});

gulp.task("sass-minify", function () {
    return gulp.src("Styles/main.scss")
        .pipe(sass({ outputStyle: "compressed" }).on("error", sass.logError))
        .pipe(gulp.dest("wwwroot/css"));
});
